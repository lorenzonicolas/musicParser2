using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;

namespace MusicParser.Utils.HttpClient
{
    [ExcludeFromCodeCoverage]
    public class HttpClient : IHttpClient
    {
        private readonly System.Net.Http.HttpClient _client = new();

        public HttpClient()
        {
            _client.DefaultRequestHeaders.Accept.Clear();
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public HttpResponseMessage Get(string url)
        {
            return GetAsync(url).Result;
        }

        public HttpResponseMessage Post(string url, HttpContent content)
        {
            return PostAsync(url, content).Result;
        }

        public async Task<HttpResponseMessage> GetAsync(string url)
        {
            return await _client.GetAsync(url);
        }

        public async Task<HttpResponseMessage> PostAsync(string url, HttpContent content)
        {
            return await _client.PostAsync(url, content);
        }

        public async Task<byte[]> GetByteArrayAsync(Uri url)
        {
            return await _client.GetByteArrayAsync(url);
        }

        public void AddHeaders(string name, string value)
        {
            _client.DefaultRequestHeaders.Add(name, value);
        }

        public void SetAuthHeaders(string scheme, string value)
        {
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(scheme, value);
        }
    }
}
