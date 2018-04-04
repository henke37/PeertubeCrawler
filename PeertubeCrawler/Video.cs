namespace PeertubeCrawler {
    public class Video {

        public string uuid;
        public string description;
        public float duration;
        public string name;

        public int views;
        public int likes;
        public int dislikes;

        public Video(dynamic obj) {
            this.uuid = obj.uuid;
            this.description = obj.description;
            this.duration = obj.duration;
            this.name = obj.name;
            this.views = obj.views;
            this.likes = obj.likes;
            this.dislikes = obj.dislikes;
        }

        public override string ToString() {
            return name;
        }
    }
}