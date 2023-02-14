using musicParser.DTO;
using musicParser.Utils.Loggers;
using System.Net;

namespace musicParser.MetalArchives
{
    public class MetalArchivesService : IMetalArchivesService
    {
        private readonly IMetalArchivesAPI metalArchivesAPI;
        private readonly IExecutionLogger _logger;

        public MetalArchivesService(
            IExecutionLogger logger,
            IMetalArchivesAPI metalAPI)
        {
            metalArchivesAPI = metalAPI;
            _logger = logger;
        }

        [Obsolete]
        public async Task<byte[]?> DownloadAlbumCover(string band, string albumToSearch)
        {
            try
            {
                var url = await GetAlbumCoverURL(band, albumToSearch);

                if (string.IsNullOrEmpty(url))
                {
                    return null;
                }

                using WebClient webClient = new();
                byte[] data = webClient.DownloadData(new Uri(url));
                return data;
            }
            catch (Exception ex)
            {
                _logger.Log($"Error trying to get AlbumCover from MetalArchives: {ex.Message}");
                return null;
            }
        }

        [Obsolete]
        public async Task<string> GetAlbumCoverURL(string band, string albumToSearch)
        {
            // TODO - returning null as I don't have a way to download the image yet
            return await Task.FromResult(string.Empty);

            //try
            //{
            //    var albumResult = await GetAlbum(band, albumToSearch);

            //    if (albumResult != null)
            //    {
            //        //Get all the album information
            //        //var response = await metalArchivesAPI.GetAlbumByID(albumResult.id);
            //        string response = null;

            //        var albumRetrieved = Newtonsoft.Json.JsonConvert.DeserializeObject<AlbumDTO>(response);

            //        return albumRetrieved.data.album.album_cover;
            //    }
            //    return null;
            //}
            //catch (Exception)
            //{
            //    return null;
            //}
        }

        public async Task<string?> GetAlbumYear(string band, string albumToSearch)
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

        public async Task<string> GetBandCountry(string bandName, string? albumName = null)
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
                        var bandDiscographyRetrieved = Newtonsoft.Json.JsonConvert.DeserializeObject<searchDiscographyResponse>(response);

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

        public async Task<string> GetBandGenre(AlbumInfoOnDisk albumInfo)
        {
            try
            {
                var results = await SearchBandByName(albumInfo.Band);

                if (results.Length == 1)
                {
                    return results.First().Genre;
                }

                //More than 1 result...lets try to filter by album
                else if (results.Length > 1)
                {
                    foreach (var band in results)
                    {
                        var response = metalArchivesAPI.GetBandDiscography(band.Id).Result;
                        var bandDiscographyRetrieved = Newtonsoft.Json.JsonConvert.DeserializeObject<searchDiscographyResponse>(response);

                        var matchedAlbum = bandDiscographyRetrieved?.data?.discography?.FirstOrDefault(x => string.Equals(x.name, albumInfo.AlbumName, StringComparison.InvariantCultureIgnoreCase));

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

            var response = Newtonsoft.Json.JsonConvert.DeserializeObject<searchBandResponse>(stringResponse);

            return response != null && response.data != null && response.data.bands != null ? response.data.bands : Array.Empty<BandResult>();
        }

        private async Task<AlbumResult?> GetAlbum(string band, string album)
        {
            var results = await SearchBandByName(band);

            foreach (var bandResult in results)
            {
                //Get all the band information from the search results
                var response = metalArchivesAPI.GetBandDiscography(bandResult.Id).Result;
                var bandDiscography = Newtonsoft.Json.JsonConvert.DeserializeObject<searchDiscographyResponse>(response);

                //Check if this band has the specified album
                var matchedAlbum = bandDiscography?.data?.discography?
                    .FirstOrDefault(x => string.Equals(x.name, album, StringComparison.InvariantCultureIgnoreCase));

                if (matchedAlbum != null)
                {
                    return matchedAlbum;
                }
            }

            return null;
        }
    }
}
