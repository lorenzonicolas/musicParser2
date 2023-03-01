using Newtonsoft.Json;

namespace musicParser.MetalArchives
{
    public class AlbumDTO
    {
        public AlbumData data { get; set; }
    }

    public class AlbumData
    {
        public BandData band { get; set; }
        public AlbumDetails album { get; set; }
        public Lineup[] personnel { get; set; }
    }

    public class AlbumDetails
    {
        public string title { get; set; }
        public string id { get; set; }
        public string type { get; set; }
        [JsonProperty(PropertyName = "release date")]
        public string release_date { get; set; }
        [JsonProperty(PropertyName = "catalog id")]
        public string catalog_id { get; set; }
        public string label { get; set; }
        public string format { get; set; }
        public string reviews { get; set; }
        public Song[] songs { set; get; }
    }

    public class Song
    {
        public string title { get; set; }
        public string length { get; set; }
    }
}
