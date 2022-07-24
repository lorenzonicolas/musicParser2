namespace musicParser.Spotify
{
    public interface ISpotifyService
    {
        List<string>? GetArtistGenre(string bandName);
        List<string>? GetArtistGenreUsingAlbum(string bandName, string album);
        string? GetAlbumYear(string albumName, string bandName);
        byte[]? DownloadAlbumCover(string band, string albumToSearch);
    }
}
