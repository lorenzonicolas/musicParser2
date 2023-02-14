using musicParser.Spotify.DTOs;
using musicParser.Utils.Loggers;
using MusicParser.Utils.HttpClient;
using System.Globalization;

namespace musicParser.Spotify
{
    public class SpotifyService : ISpotifyService
    {
        private readonly IExecutionLogger logger;
        private readonly ISpotifyAPI api;
        private readonly IHttpClient http;

        public SpotifyService(
            IExecutionLogger ExLogger,
            ISpotifyAPI spotifyAPI,
            IHttpClient httpClient)
        {
            logger = ExLogger;
            api = spotifyAPI;
            http = httpClient;
        }

        public List<string>? GetArtistGenre(string bandName)
        {
            try
            {
                var searchResult = api.SearchBand(bandName);

                if (searchResult?.Artists?.Total == 1)
                {
                    return searchResult.Artists.Items[0]?.Genres;
                }

                return null;
            }
            catch (Exception ex)
            {
                logger.Log($"Error trying to get Artist genre Spotify: {ex.Message}");
                return null;
            }
        }

        // FUCK ME - SPOTIFY NO DEVUELVE COUNTRY ANYMORE.
        //public List<string> GetArtistCountry(string bandName)
        //{
        //    try
        //    {
        //        var searchResult = api.SearchBand(bandName);

        //        if (searchResult.Artists.Total == 1)
        //        {
        //            return searchResult.Artists.Items[0].Genres;
        //        }

        //        return null;
        //    }
        //    catch (Exception ex)
        //    {
        //        logger.Log($"Error trying to get Artist genre Spotify: {ex.Message}");
        //        return null;
        //    }
        //}

        public List<string>? GetArtistGenreUsingAlbum(string bandName, string album)
        {
            try
            {
                ClearAlbumString(album);

                var albumInfo = GetAlbum(album, bandName);

                if (albumInfo == null)
                //Didn't work. Lets at least try to get the genre using only the band name
                {
                    return GetArtistGenre(bandName);
                }
                else
                {
                    var bandRetrieved = GetBandById(albumInfo.Artists[0].Id);

                    return bandRetrieved?.Genres;
                }
            }
            catch (Exception ex)
            {
                logger.Log($"Error trying to get Artist genre Spotify: {ex.Message}");
                return null;
            }
        }

        public string? GetAlbumYear(string albumName, string bandName)
        {
            try
            {
                var album = GetAlbum(albumName, bandName);

                return album != null
                    ? DateTime.ParseExact(album.ReleaseDate, "yyyy-MM-dd", CultureInfo.InvariantCulture).Year.ToString()
                    : null;
            }
            catch (Exception ex)
            {
                logger.Log($"Error trying to get Album year from Spotify: {ex.Message}");
                return null;
            }
        }

        public byte[]? DownloadAlbumCover(string band, string albumToSearch)
        {
            try
            {
                var album = GetAlbum(albumToSearch, band);

                string url;

                if (album != null && album.Images.Count > 0)
                {
                    url = album.Images[0].Url;
                }
                else
                {
                    return null;
                }

                return http.GetByteArrayAsync(new Uri(url)).Result;
            }
            catch (Exception ex)
            {
                logger.Log($"Error trying to get AlbumCover from Spotify: {ex.Message}");
                return null;
            }
        }

        private AlbumDTO? GetAlbum(string albumName, string bandName)
        {
            var searchResult = api.SearchAlbum(albumName, bandName);

            if (searchResult?.Albums.Total == 1 && searchResult.Albums.Items[0].Artists.Count == 1)
            {
                return searchResult.Albums.Items[0];
            }

            return null;
        }

        private BandDTO? GetBandById(string bandId)
        {
            return api.SearchBandById(bandId);
        }

        private static void ClearAlbumString(string album)
        {
            album
                .Clear("Album")
                .Clear("EP")
                .Clear("Live")
                .Clear("Compilation");
        }
    }

    static class Extensions
    {
        public static string Clear(this string album, string key)
        {
            var strToReplace = $"({key})";
            var str2ToReplace = $"[{key}]";

            return album.Replace(strToReplace, string.Empty).Replace(str2ToReplace, string.Empty);
        }
    }
}

