using Newtonsoft.Json;

namespace musicParser.MetalArchives
{
    public class searchBandResponse : baseDTO
    {
        public SearchBand data { get; set; }
    }

    public class SearchBand
    {
        public int totalResults { get; set; }
        public int currentResult { get; set; }
        public BandResult[] bands { get; set; }
    }

    public class BandResult
    {
        [JsonProperty(PropertyName = "band_name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "band_genre")]
        public string Genre { get; set; }

        [JsonProperty(PropertyName = "band_id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "band_country")]
        public string Country { get; set; }
    }
}