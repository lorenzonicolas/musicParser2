using musicParser.DTO;
using System.Collections.Generic;

namespace musicParser.Metadata
{
    public interface IMetadataService
    {
        bool SyncMetadataFile(List<AlbumInfoOnDisk> allAlbums);
        string GetBandGenre(string band);
        string GetBandCountry(string band);
        bool UpdateMetadataFile();
        List<MetadataDto> GetCountryMetadataToFix();
    }
}
