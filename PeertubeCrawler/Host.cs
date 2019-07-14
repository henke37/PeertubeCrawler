using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace PeertubeCrawler {
    internal class Host {
        private string hostName;

        public List<Host> followees;
        public List<Host> followers;
        public List<Video> videos;

        public string HostName { get => hostName; }

        public Host(string hostName) {
            this.hostName = hostName;
        }

        public Task UpdateData(Crawler crawler) {
            var t1= UpdateFollowees(crawler);
            var t2= UpdateFollowers(crawler);

            return Task.WhenAll(t1,t2);
        }

        public async Task UpdateFollowees(Crawler crawler) {
            followees = new List<Host>();

            int page = 0;
            for(; ; ) {

                var obj = await fetchFollowees(page);

                foreach(var subobj in obj.data) {
                    dynamic following = subobj.following;
                    followees.Add(crawler.GetHost(following.host.ToString()));
                }


                if(followees.Count >= obj.Value<int>("total")) break;
                page += obj.data.Count;
            }
        }

        public async Task UpdateFollowers(Crawler crawler) {
            followers = new List<Host>();

            int page = 0;
            for(; ;) {
                var obj = await fetchFollowers(page);
                foreach(var subobj in obj.data) {
                    dynamic follower = subobj.follower;
                    followers.Add(crawler.GetHost(follower.host.ToString()));
                }

                if(followers.Count >= obj.Value<int>("total")) break;
                page += obj.data.Count;
            } 
        }

        public async Task UpdateVideos() {
            videos = new List<Video>();

            int page = 0;
            for(; ; ) {
                var obj = await fetchVideos(page);
                foreach(var subobj in obj.data) {
                    var vid = new Video(subobj);
                    videos.Add(vid);
                }

                if(videos.Count >= obj.Value<int>("total")) break;
                page += obj.data.Count;
            }
        }

        private async Task<dynamic> fetchUrl(string url) {
            var client = new HttpClient();
            client.Timeout = new System.TimeSpan(0, 0, 25);
            var resp = await client.GetAsync(url);
            var str = await resp.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject(str);
        }

        private async Task<dynamic> fetchFollowees(int page) {
            var url = "https://" + hostName + "/api/v1/server/following?start=" + page;
            return await fetchUrl(url);
        }

        private async Task<dynamic> fetchFollowers(int page) {
            var url = "https://" + hostName + "/api/v1/server/followers?start=" + page;
            return await fetchUrl(url);
        }

        private async Task<dynamic> fetchVideos(int page) {
            var url = "https://" + hostName + "/api/v1/videos?filter=local&start=" + page;
            return await fetchUrl(url);
        }

        public override string ToString() => hostName;
    }
}