namespace musicParser.Spotify.DTOs
{
    public class BandDTO
    {
        public List<string> Genres { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }
    }

    public class BandSearchDTO
    {
        public List<BandDTO> Items { get; set; }
        public int Total { get; set; }
    }

    public class SearchBandResponse
    {
        public BandSearchDTO Artists { get; set; }
    }
}
