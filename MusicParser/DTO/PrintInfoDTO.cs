namespace musicParser.DTO
{
    public class Album
    {
        public string Title { get; set; }
        public int Year { get; set; }
        public string Genre { get; set; }
    }

    public class Band
    {
        public List<Album> Albums { get; set; }
        public string Country { get; set; }
        public string Name { get; set; }
    }
}
