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
            var t=c.Crawl();
            t.Wait();

            Graph graph=c.MakeGraph();

            string gs = graph.ToString("X", new DOTFormatProvider());
            File.WriteAllText("graph.dot", gs);
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
                if(host.followees!=null) {
                    foreach(var followee in host.followees) {
                        int followeeId = hostIds[followee];
                        graph.AddEdge(followeeId, hostId);
                    }
                }
                if(host.followers!=null) {
                    foreach(var follower in host.followers) {
                        int followerId = hostIds[follower];
                        graph.AddEdge(hostId, followerId);
                    }
                }
            }

            return graph;
        }

        public async Task Crawl() {
            for(; ;) {
                Host nextHost;
                if(!hostsToCrawl.TryDequeue(out nextHost)) break;

                Console.WriteLine(nextHost.HostName);

                try {
                    await nextHost.UpdateFollowees(this);
                    await nextHost.UpdateFollowers(this);
                } catch(Exception ex) {
                    Console.Out.WriteLine(ex.Message);
                }
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
