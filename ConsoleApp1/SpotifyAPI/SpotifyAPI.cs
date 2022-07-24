using musicParser.Spotify;
using musicParser.Spotify.DTOs;
using musicParser.Utils.Regex;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace musicParser.Spotify
{
    public class SpotifyAPIimplemen : ISpotifyAPI
    {
        private readonly IRegexUtils RegexUtils;
        private readonly HttpClient client;

        private readonly string loginUrl = "https://accounts.spotify.com/api/token";
        private readonly string searchUrl = "https://api.spotify.com/v1/search?q={0}&type={1}";
        private readonly string bandByIDUrl = "https://api.spotify.com/v1/artists/{0}";
        private readonly string accessToken;

        public SpotifyAPIimplemen(IRegexUtils regexUtils)
        {
            RegexUtils = regexUtils;
            client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            accessToken = GetAccessToken();
        }

        public SearchBandResponse SearchBand(string bandName)
        {
            bandName = RegexUtils.ReplaceAllSpaces(bandName);

            var url = string.Format(searchUrl, bandName, "artist");
            var result = CallerSync(url, WebRequestMethods.Http.Get);

            return JsonConvert.DeserializeObject<SearchBandResponse>(result);
        }

        public SearchBandResponse SearchBand(string bandName, string genre)
        {
            bandName = RegexUtils.ReplaceAllSpaces(bandName);
            genre = RegexUtils.ReplaceAllSpaces(genre);

            var query = $"{bandName}%20genre:%22{genre}%22";
            var url = string.Format(searchUrl, query, "artist");
            var result = CallerSync(url, WebRequestMethods.Http.Get);

            return JsonConvert.DeserializeObject<SearchBandResponse>(result);
        }

        public BandDTO SearchBandById(string id)
        {
            var url = string.Format(bandByIDUrl, id);

            var result = CallerSync(url, WebRequestMethods.Http.Get);
            return JsonConvert.DeserializeObject<BandDTO>(result);
        }

        public SearchAlbumResponse SearchAlbum(string albumName)
        {
            albumName = RegexUtils.ReplaceAllSpaces(albumName);

            var url = string.Format(searchUrl, albumName, "album");
            var result = CallerSync(url, WebRequestMethods.Http.Get);

            return JsonConvert.DeserializeObject<SearchAlbumResponse>(result);
        }

        public SearchAlbumResponse SearchAlbum(string albumName, string bandName)
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

            var response = CallerSync(loginUrl, WebRequestMethods.Http.Post, new FormUrlEncodedContent(data), isLogin:true);
            var loginResponse = JsonConvert.DeserializeObject<LoginDTO>(response);

            return loginResponse.AccessToken;
        }

        private string CallerSync(string url, string operation, HttpContent content = null, bool isLogin = false)
        {
            if (isLogin)
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", BuildBasicHeader());
            else
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            }

            HttpResponseMessage response;

            if (operation == WebRequestMethods.Http.Get)
                response = client.GetAsync(url).Result;
            else if (operation == WebRequestMethods.Http.Post)
                response = client.PostAsync(url, content).Result;
            else
                throw new Exception("Invalid caller operation");

            if (response.IsSuccessStatusCode)
            {
                if (isLogin) client.DefaultRequestHeaders.Remove("Authorization");

                return response.Content.ReadAsStringAsync().Result;
            }
            else
                throw new Exception("Communication error");
        }

        private string BuildBasicHeader()
        {
            var clientId = ConfigurationManager.AppSettings["spotifyfClientID"].ToString();
            var clientSecret = "af87ed2121bd4d58b9108f63e51c773f";
            var header = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{clientId}:{clientSecret}"));
            return header;
        }
    }
}