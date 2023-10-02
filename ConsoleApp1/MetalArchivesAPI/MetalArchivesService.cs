using musicParser.Utils.Loggers;
using MusicParser.Utils.HttpClient;
using Newtonsoft.Json;

namespace musicParser.MetalArchives
{
    public class MetalArchivesService : IMetalArchivesService
    {
        private readonly IMetalArchivesAPI metalArchivesAPI;
        private readonly IExecutionLogger _logger;
        private readonly IHttpClient httpClient;

        public MetalArchivesService(
            IExecutionLogger logger,
            IMetalArchivesAPI metalAPI,
            IHttpClient http)
        {
            metalArchivesAPI = metalAPI;
            _logger = logger;
            httpClient = http;
        }

        public async Task<byte[]?> DownloadAlbumCoverAsync(string band, string albumToSearch)
        {
            if(string.IsNullOrEmpty(band) || string.IsNullOrEmpty(albumToSearch))
            {
                return null;
            }
            
            try
            {
                var url = await GetAlbumCoverURL(band, albumToSearch);

                if (string.IsNullOrEmpty(url))
                {
                    return null;
                }

                byte[] data = await httpClient.GetByteArrayAsync(new Uri(url));
                return data;
            }
            catch (Exception ex)
            {
                _logger.Log($"Error trying to get AlbumCover from MetalArchives: {ex.Message}");
                return null;
            }
        }

        private async Task<string?> GetAlbumCoverURL(string band, string albumToSearch)
        {
            try
            {
                var albumResult = await GetAlbum(band, albumToSearch);

                if(albumResult == null)
                {
                    return null;
                }

                return await GetAlbumCoverURL(albumResult.id);
            }
            catch (Exception ex)
            {
                _logger.Log($"Error trying to get Album cover from MetalArchives: {ex.Message}");
                return null;
            }
        }

        private async Task<string?> GetAlbumCoverURL(string albumId)
        {
            var stringResponse = await metalArchivesAPI.GetAlbumCoverUrl(albumId);

            var response = JsonConvert.DeserializeObject<searchAlbumCoverResponse>(stringResponse);

            return response?.data?.album.coverUrl;
        }

        public async Task<string?> GetAlbumYearAsync(string band, string albumToSearch)
        {
            try
            {
                var result = await GetAlbum(band, albumToSearch);

                return result?.year;
            }
            catch (Exception ex)
            {
                _logger.Log($"Error trying to get AlbumYear from MetalArchives: {ex.Message}");
                return null;
            }            
        }

        public async Task<string> GetBandCountryAsync(string bandName, string? albumName = null)
        {
            try
            {
                var results = await SearchBandByName(bandName);

                if (results.Length == 1)
                {
                    return results.First().Country;
                }

                //More than 1 result... lets try to filter by album if I received one
                else if (results.Length > 1 && !string.IsNullOrEmpty(albumName))
                {
                    foreach (var band in results)
                    {
                        var response = metalArchivesAPI.GetBandDiscography(band.Id).Result;
                        var bandDiscographyRetrieved = JsonConvert.DeserializeObject<searchDiscographyResponse>(response);

                        var matchedAlbum = bandDiscographyRetrieved?.data?.discography?.FirstOrDefault(x => string.Equals(x.name, albumName, StringComparison.InvariantCultureIgnoreCase));

                        if (matchedAlbum != null)
                        {
                            return band.Country;
                        }
                    }
                }

                return "Unknown";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error looking for {bandName} country: {ex.Message}");
                return "Unknown";
            }
        }

        public async Task<string> GetBandGenreAsync(string bandName, string? albumName = null)
        {
            try
            {
                var results = await SearchBandByName(bandName);

                if (results.Length == 1)
                {
                    return results.First().Genre;
                }

                //More than 1 result...lets try to filter by album
                else if (results.Length > 1 && !string.IsNullOrEmpty(albumName))
                {
                    foreach (var band in results)
                    {
                        var response = metalArchivesAPI.GetBandDiscography(band.Id).Result;
                        var bandDiscographyRetrieved = JsonConvert.DeserializeObject<searchDiscographyResponse>(response);

                        var matchedAlbum = bandDiscographyRetrieved?.data?.discography?.FirstOrDefault(x => string.Equals(x.name, albumName, StringComparison.InvariantCultureIgnoreCase));

                        if (matchedAlbum != null)
                        {
                            return band.Genre;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Log($"Error trying to get genre from MetalArchives: {ex.Message}");
                return string.Empty;
            }

            return string.Empty;
        }

        private async Task<BandResult[]> SearchBandByName(string band)
        {
            band = band.Replace(" ", "%20");
            var stringResponse = await metalArchivesAPI.Search("name", band);

            var response = JsonConvert.DeserializeObject<searchBandResponse>(stringResponse);

            return response?.data?.bands ?? Array.Empty<BandResult>();
        }

        private async Task<AlbumResult?> GetAlbum(string band, string album)
        {
            var results = await SearchBandByName(band);

            foreach (var bandResult in results)
            {
                try
                {
                    // Get all the band information from the search results
                    var response = metalArchivesAPI.GetBandDiscography(bandResult.Id).Result;
                    var bandDiscography = JsonConvert.DeserializeObject<searchDiscographyResponse>(response);

                    // Check if this band has the specified album
                    var matchedAlbum = bandDiscography?.data?.discography?
                        .FirstOrDefault(x => string.Equals(x.name, album, StringComparison.InvariantCultureIgnoreCase));

                    if (matchedAlbum != null)
                    {
                        return matchedAlbum;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Log($"Error trying to get this band discography. Will skip this band ({bandResult.Name}) in order to get the album. Error: {ex.Message}");
                }                
            }

            return null;
        }
    }
}
