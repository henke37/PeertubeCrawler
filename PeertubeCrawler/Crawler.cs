using Mendz.Graph;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PeertubeCrawler {
    class Crawler {

        private Dictionary<string, Host> hosts;
        private ConcurrentQueue<Host> hostsToCrawl;

        public Crawler() {
            hosts = new Dictionary<string, Host>();
            hostsToCrawl = new ConcurrentQueue<Host>();
        }

        static void Main(string[] args) {
            var c = new Crawler();
            c.GetHost(args[0]);
            Task t = c.CrawlHosts();
            t.Wait();

            t = c.CrawlVideos();
            t.Wait();

            Graph graph = c.MakeGraph();

            string gs = graph.ToString("X", new DOTFormatProvider());
            var now = DateTime.Now;
            File.WriteAllText($"graph-{now.ToString("yyyy-MM-dd")}.gv", gs);

            DumpHosts(c.hosts);
            DumpVideos(c);
        }

        private static void DumpVideos(Crawler c) {
            var now = DateTime.Now;
            string path = $"videos-{now.ToString("yyyy-MM-dd")}.csv";
            using(var w = new StreamWriter(File.OpenWrite(path))) {
                foreach(var kv in c.hosts) {
                    var host = kv.Value;

                    if(host.videos == null) continue;

                    foreach(var video in host.videos) {
                        w.WriteLine($"{host.HostName},{video.uuid},\"{escape(video.name)}\",{video.duration},{video.language},{video.user},{video.views},{video.publishDate}");
                    }
                }
            }
        }

        private static string escape(string name) {
            return name.Replace("\"", "\"\"");
        }

        private static void DumpHosts(Dictionary<string, Host> hosts) {
            var now = DateTime.Now;
            string path = $"hosts-{now.ToString("yyyy-MM-dd")}.txt";

            using(var w=new StreamWriter(File.OpenWrite(path))) {
                foreach(var kv in hosts) {
                    var host = kv.Value;
                    w.WriteLine(host.HostName);
                }
            }
        }

        public Graph MakeGraph() {
            Graph graph = new Graph();

            int nextNodeId = 0;

            Dictionary<Host, int> hostIds = new Dictionary<Host, int>();
            foreach(var kv in hosts) {
                Host host = kv.Value;
                int nodeId = nextNodeId++;
                hostIds.Add(host, nodeId);

                graph.AddVertex(nodeId, host.HostName);
            }

            foreach(var kv in hosts) {
                Host host = kv.Value;
                int hostId = hostIds[host];
                if(host.followees != null) {
                    foreach(var followee in host.followees) {
                        int followeeId = hostIds[followee];
                        graph.AddEdge(followeeId, hostId, true);
                    }
                }
                if(host.followers != null) {
                    foreach(var follower in host.followers) {
                        int followerId = hostIds[follower];
                        graph.AddEdge(hostId, followerId, true);
                    }
                }
            }

            return graph;
        }

        public async Task CrawlHosts() {
            for(; ; ) {
                var PendingTasks = new List<Task>();

                while(!hostsToCrawl.IsEmpty) {
                    Host nextHost;
                    if(!hostsToCrawl.TryDequeue(out nextHost)) break;

                    Task t = Task.Run(async () => {

                        Output(nextHost.HostName);

                        try {
                            await nextHost.UpdateData(this);
                        } catch(Exception ex) {
                            Output(ex.Message);
                        }
                    });

                    PendingTasks.Add(t);
                }
                await Task.WhenAll(PendingTasks);
                if(hostsToCrawl.IsEmpty) break;
            }

        }

        public Task CrawlVideos() {
            var videoTasks = new List<Task>();
            foreach(var kv in hosts) {
                var host = kv.Value;
                Task t = Task.Run(async () => {

                    Output(host.HostName);

                    try {
                        await host.UpdateVideos();
                    } catch(Exception ex) {
                        Output(ex.Message);
                    }
                });
                videoTasks.Add(t);
            }
            return Task.WhenAll(videoTasks);
        }

        private void Output(string message) {
            lock(Console.Out) {
                Console.Out.WriteLine($"[{System.Threading.Thread.CurrentThread.ManagedThreadId}] {message}");
            }
        }

        public Host GetHost(string hostName) {
            lock(hosts) {
                Host host;
                if(hosts.TryGetValue(hostName, out host)) return host;
                host = new Host(hostName);
                hosts.Add(hostName, host);
                hostsToCrawl.Enqueue(host);
                return host;
            }
        }
    }
}
