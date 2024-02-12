using musicParser.Spotify.DTOs;

namespace musicParser.Spotify
{
    public interface ISpotifyAPI
    {
        SearchBandResponse? SearchBand(string bandName);
        SearchBandResponse? SearchBand(string bandName, string genre);
        BandDTO? SearchBandById(string id);
        SearchAlbumResponse? SearchAlbum(string albumName);
        SearchAlbumResponse? SearchAlbum(string albumName, string bandName);
    }
}
