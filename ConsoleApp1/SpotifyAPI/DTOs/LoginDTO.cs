using Newtonsoft.Json;

namespace musicParser.Spotify.DTOs
{
    public class LoginDTO
    {
        [JsonProperty(PropertyName = "access_token")]
        public string AccessToken { get; set; }
    }
}
