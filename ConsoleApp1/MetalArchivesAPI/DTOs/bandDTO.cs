using Newtonsoft.Json;

namespace musicParser.MetalArchives
{
    public class BandDTO
    {
        public BandData data { get; set; }
    }

    public class BandData
    {
        public string id { get; set; }
        public BandDetails details { get; set; }
        public string band_name { get; set; }
        public string logo { get; set; }
        public string photo { get; set; }
        public string bio { get; set; }
        public Discography[] discography { get; set; }
        public Lineup[] current_lineup { get; set; }
    }

    public class Lineup
    {
        public string name { get; set; }
        public string id { get; set; }
        public string instrument { get; set; }
        public string years { get; set; }
    }

    public class Discography
    {
        public string id { get; set; }
        public string name { get; set; }
        public string type { get; set; }
        public string year { get; set; }
    }

    public class BandDetails
    {
        [JsonProperty(PropertyName = "country of origin")]
        public string country_of_origin { get; set; }
        public string location { get; set; }
        public string status { get; set; }
        [JsonProperty(PropertyName = "formed in")]

        public string formed_in { get; set; }
        public string genre { get; set; }
        [JsonProperty(PropertyName = "lyrical themes")]
        public string lyrical_themes { get; set; }
        [JsonProperty(PropertyName = "current label")]
        public string current_label { get; set; }
        [JsonProperty(PropertyName = "years active")]
        public string years_active { get; set; }
    }
}
