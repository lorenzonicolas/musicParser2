using System.Diagnostics.CodeAnalysis;

namespace MusicParser.Utils.HttpClient
{
    [ExcludeFromCodeCoverage]
    public class HttpClient : IHttpClient
    {
        private readonly HttpClient _client = new();

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
    }
}
