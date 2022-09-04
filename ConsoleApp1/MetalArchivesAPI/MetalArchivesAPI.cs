using System.Net.Http.Headers;

namespace musicParser.MetalArchives
{
    public class MetalArchivesAPI : IMetalArchivesAPI
    {
        private readonly HttpClient client;

        //private readonly string searchURL = "http://em.wemakesites.net/search/{0}/{1}?api_key={2}";
        //private readonly string bandURL = "http://em.wemakesites.net/band/{0}?api_key={1}";
        //private readonly string albumURL = "http://em.wemakesites.net/album/{0}?api_key={1}";

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

        private async Task<string> Caller(string url)
        {
            HttpResponseMessage response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var stringResponse = await response.Content.ReadAsStringAsync();
                return stringResponse;
            }
            else
                throw new Exception("Communication error");
        }
    }
}