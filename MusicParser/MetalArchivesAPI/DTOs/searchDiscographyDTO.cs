namespace musicParser.MetalArchives
{
    public class searchDiscographyResponse : baseDTO
    {
        public SearchDiscography data { get; set; }
    }

    public class SearchDiscography
    {
        public AlbumResult[] discography { get; set; }
    }

    public class AlbumResult
    {
        public string id { get; set; }
        public string name { get; set; }
        public string type { get; set; }
        public string year { get; set; }
    }
}