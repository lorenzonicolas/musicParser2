namespace MusicParser.Utils.HttpClient
{
    public interface IHttpClient
    {
        HttpResponseMessage Get(string url);
        HttpResponseMessage Post(string url, HttpContent content);
        Task<HttpResponseMessage> GetAsync(string url);
        Task<HttpResponseMessage> PostAsync(string url, HttpContent content);
        Task<byte[]> GetByteArrayAsync(Uri url);
    }
}
