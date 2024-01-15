using Newtonsoft.Json;
using System.Collections.Generic;

namespace musicParser.Spotify.DTOs
{
    public class SearchAlbumResponse
    {
        public AlbumSearchDTO Albums { get; set; }
    }

    public class AlbumSearchDTO
    {
        public List<AlbumDTO> Items { get; set; }
        public int Total { get; set; }
    }

    public class AlbumDTO
    {
        [JsonProperty(PropertyName = "album_type")]
        public string AlbumType { get; set; }
        public List<BandDTO> Artists { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }
        [JsonProperty(PropertyName = "release_date")]
        public string ReleaseDate { get; set; }
        public List<AlbumImageDTO> Images { get; set; }
    }

    public class AlbumImageDTO
    {
        public int Height { get; set; }
        public int Width { get; set; }
        public string Url { get; set; }
    }
}