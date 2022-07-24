using System.Threading.Tasks;

namespace musicParser.MetalArchives
{
    public interface IMetalArchivesAPI
    {
        Task<string> Search(string searchType, string keyword);
        Task<string> GetBandByID(string id);
        Task<string> GetBandDiscography(string bandId);
    }
}
