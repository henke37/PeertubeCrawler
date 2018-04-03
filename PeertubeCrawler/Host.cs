using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace PeertubeCrawler {
    internal class Host {
        private string hostName;

        public List<Host> followees;
        public List<Host> followers;

        public string HostName { get => hostName; }

        public Host(string hostName) {
            this.hostName = hostName;
        }

        public async Task UpdateFollowees(Crawler crawler) {
            followees = new List<Host>();

            var obj = await fetchFollowees();

            foreach(var subobj in obj.data) {
                dynamic following = subobj.following;
                followees.Add(crawler.GetHost(following.host.ToString()));
            }
        }

        public async Task UpdateFollowers(Crawler crawler) {
            followers = new List<Host>();

            var obj = await fetchFollowers();
            foreach(var subobj in obj.data) {
                dynamic follower = subobj.follower;
                followers.Add(crawler.GetHost(follower.host.ToString()));
            }
        }

        private async Task<dynamic> fetchUrl(string url) {
            var client = new HttpClient();
            client.Timeout = new System.TimeSpan(0, 0, 5);
            var resp = await client.GetAsync(url);
            var str = await resp.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject(str);
        }

        private async Task<dynamic> fetchFollowees() {
            var url = "https://" + hostName + "/api/v1/server/following";
            return await fetchUrl(url);
        }

        private async Task<dynamic> fetchFollowers() {
            var url = "https://" + hostName + "/api/v1/server/followers";
            return await fetchUrl(url);
        }

        public override string ToString() => hostName;
    }
}