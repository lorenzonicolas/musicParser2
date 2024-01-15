namespace musicParser.MetalArchives
{
    public class searchAlbumCoverResponse : baseDTO
    {
        public SearchAlbumCover data { get; set; }
    }

    public class SearchAlbumCover
    {
        public AlbumCoverResult album { get; set; }
    }

    public class AlbumCoverResult
    {
        public string coverUrl { get; set; }
    }
}