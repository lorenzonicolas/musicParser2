using Microsoft.Extensions.Configuration;
using musicParser.Spotify.DTOs;
using musicParser.Utils.Regex;
using MusicParser.Utils.HttpClient;
using Newtonsoft.Json;
using System.Net;
using System.Text;

namespace musicParser.Spotify
{
    public class SpotifyAPIimplemen : ISpotifyAPI
    {
        private readonly IRegexUtils RegexUtils;
        private readonly IHttpClient client;

        public static readonly string loginUrl = "https://accounts.spotify.com/api/token";
        private readonly string searchUrl = "https://api.spotify.com/v1/search?q={0}&type={1}";
        private readonly string bandByIDUrl = "https://api.spotify.com/v1/artists/{0}";
        private readonly string accessToken;
        private readonly string clientId;
        private readonly string clientSecret;

        public SpotifyAPIimplemen(
            IRegexUtils regexUtils,
            IConfiguration config,
            IHttpClient httpClient)
        {
            RegexUtils = regexUtils;
            client = httpClient;
            clientId = config.GetValue<string>("spotifyfClientID");
            clientSecret = config.GetValue<string>("spotifyfClientSecret");
            accessToken = GetAccessToken();
        }

        public SearchBandResponse? SearchBand(string bandName)
        {
            bandName = RegexUtils.ReplaceAllSpaces(bandName);

            var url = string.Format(searchUrl, bandName, "artist");
            var result = CallerSync(url, WebRequestMethods.Http.Get);

            return JsonConvert.DeserializeObject<SearchBandResponse>(result);
        }

        public SearchBandResponse? SearchBand(string bandName, string genre)
        {
            bandName = RegexUtils.ReplaceAllSpaces(bandName);
            genre = RegexUtils.ReplaceAllSpaces(genre);

            var query = $"{bandName}%20genre:%22{genre}%22";
            var url = string.Format(searchUrl, query, "artist");
            var result = CallerSync(url, WebRequestMethods.Http.Get);

            return JsonConvert.DeserializeObject<SearchBandResponse>(result);
        }

        public BandDTO? SearchBandById(string id)
        {
            var url = string.Format(bandByIDUrl, id);

            var result = CallerSync(url, WebRequestMethods.Http.Get);
            return JsonConvert.DeserializeObject<BandDTO>(result);
        }

        public SearchAlbumResponse? SearchAlbum(string albumName)
        {
            albumName = RegexUtils.ReplaceAllSpaces(albumName);

            var url = string.Format(searchUrl, albumName, "album");
            var result = CallerSync(url, WebRequestMethods.Http.Get);

            return JsonConvert.DeserializeObject<SearchAlbumResponse>(result);
        }

        public SearchAlbumResponse? SearchAlbum(string albumName, string bandName)
        {
            albumName = RegexUtils.ReplaceAllSpaces(albumName);
            bandName = RegexUtils.ReplaceAllSpaces(bandName);

            var query = $"album:{albumName}%20artist:{bandName}";
            var url = string.Format(searchUrl, query, "album");
            var result = CallerSync(url, WebRequestMethods.Http.Get);

            return JsonConvert.DeserializeObject<SearchAlbumResponse>(result);
        }

        private string GetAccessToken()
        {
            var data = new Dictionary<string, string> { { "grant_type", "client_credentials" } };

            var response = CallerSync(loginUrl, WebRequestMethods.Http.Post, new FormUrlEncodedContent(data), isLogin: true);
            var loginResponse = JsonConvert.DeserializeObject<LoginDTO>(response);

            if (loginResponse == null)
            {
                throw new Exception("Error getting the access token");
            }

            return loginResponse.AccessToken;
        }

        private string CallerSync(string url, string operation, HttpContent? content = null, bool isLogin = false)
        {
            if (isLogin)
            {
                client.SetAuthHeaders("Basic", BuildBasicHeader());
            }
            else
            {
                client.SetAuthHeaders("Bearer", accessToken);
            }

            HttpResponseMessage response;

            if (operation == WebRequestMethods.Http.Get)
            {
                response = client.Get(url);
            }
            else if (operation == WebRequestMethods.Http.Post)
            {
#pragma warning disable CS8604 // Possible null reference argument.
                response = client.Post(url, content);
#pragma warning restore CS8604 // Possible null reference argument.
            }
            else
            {
                throw new Exception("Invalid caller operation");
            }

            if (response.IsSuccessStatusCode)
            {
                return response.Content.ReadAsStringAsync().Result;
            }
            else
            {
                throw new Exception("Communication error");
            }
        }

        private async Task<string> CallerAsync(string url, string operation, HttpContent? content = null, bool isLogin = false)
        {
            if (isLogin)
            {
                client.SetAuthHeaders("Basic", BuildBasicHeader());
            }
            else
            {
                client.SetAuthHeaders("Bearer", accessToken);
            }

            HttpResponseMessage response;

            if (operation == WebRequestMethods.Http.Get)
            {
                response = await client.GetAsync(url);
            }
            else if (operation == WebRequestMethods.Http.Post)
            {
#pragma warning disable CS8604 // Possible null reference argument.
                response = await client.PostAsync(url, content);
#pragma warning restore CS8604 // Possible null reference argument.
            }
            else
            {
                throw new Exception("Invalid caller operation");
            }

            try
            {
                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("Communication error: " + ex.Message, ex);
            }
        }

        private string BuildBasicHeader()
        {
            return Convert.ToBase64String(Encoding.ASCII.GetBytes($"{clientId}:{clientSecret}"));
        }
    }
}