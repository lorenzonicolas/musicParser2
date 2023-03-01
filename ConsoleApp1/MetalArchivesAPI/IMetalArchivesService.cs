namespace musicParser.MetalArchives
{
    public interface IMetalArchivesService
    {
        Task<string?> GetAlbumYearAsync(string band, string albumToSearch);
        Task<string> GetBandCountryAsync(string bandName, string? albumName = null);
        Task<string> GetBandGenreAsync(string bandName, string? albumName = null);
        Task<byte[]?> DownloadAlbumCoverAsync(string band, string albumToSearch);
    }
}
