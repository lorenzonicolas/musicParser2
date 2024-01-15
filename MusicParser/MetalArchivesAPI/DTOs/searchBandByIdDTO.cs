using Newtonsoft.Json;

namespace musicParser.MetalArchives
{
    public class searchBandByIdResponse : baseDTO
    {
        public SearchBand data { get; set; }
    }

    public class SearchBandById
    {
        public int totalResults { get; set; }
        public int currentResult { get; set; }
        public FullBandResult band { get; set; }
    }

    public class FullBandResult
    {
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "genre")]
        public string Genre { get; set; }

        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "country")]
        public string Country { get; set; }
    }
}