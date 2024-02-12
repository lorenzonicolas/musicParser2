using System.Net.Http.Headers;

namespace musicParser.MetalArchives
{
    public class MetalArchivesAPI : IMetalArchivesAPI
    {
        private readonly HttpClient client;
        private readonly string bandURL = "http://localhost:3000/bands";

        public MetalArchivesAPI()
        {
            client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<string> Search(string searchType, string keyword)
        {
            var url = $"{bandURL}?{searchType}={keyword}";
            return await Caller(url);
        }

        public async Task<string> GetBandByID(string id)
        {
            var url = $"{bandURL}/{id}";
            return await Caller(url);
        }

        public async Task<string> GetBandDiscography(string bandId)
        {
            var url = $"{bandURL}/{bandId}/discography";
            return await Caller(url);
        }

        public async Task<string> GetAlbumCoverUrl(string albumId)
        {
            var url = $"{bandURL}/album/{albumId}";
            return await Caller(url);
        }

        private async Task<string> Caller(string url)
        {
            HttpResponseMessage response = await client.GetAsync(url);

            switch (response.StatusCode)
            {
                case System.Net.HttpStatusCode.OK:
                    var stringResponse = await response.Content.ReadAsStringAsync();
                    return stringResponse;
                case System.Net.HttpStatusCode.NotFound:
                    throw new Exception("Content not found");
                default:
                    throw new Exception("Communication error");
            }
        }
    }
}