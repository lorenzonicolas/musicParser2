namespace musicParser.MetalArchives
{
    public interface IMetalArchivesAPI
    {
        Task<string> Search(string searchType, string keyword);
        Task<string> GetBandByID(string id);
        Task<string> GetBandDiscography(string bandId);
        Task<string> GetAlbumCoverUrl(string albumId);
    }
}
