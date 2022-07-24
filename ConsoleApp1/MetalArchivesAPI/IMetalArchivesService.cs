using musicParser.DTO;
using System;
using System.Threading.Tasks;

namespace musicParser.MetalArchives
{
    public interface IMetalArchivesService
    {
        Task<string> GetAlbumYear(string band, string albumToSearch);
        Task<string> GetBandCountry(string bandName, string albumName = null);
        Task<string> GetBandGenre(AlbumInfoOnDisk albumInfo);

        [Obsolete]
        Task<byte[]> DownloadAlbumCover(string band, string albumToSearch);
        [Obsolete]
        Task<string> GetAlbumCoverURL(string band, string albumToSearch);
    }
}
