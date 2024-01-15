namespace musicParser.GoogleDrive
{
    public interface IGoogleDriveService
    {
        void DeleteFile(string id);
        List<DTO.AlbumInfoBackupDto> GetBackupFile();
        bool UpdateBackupFile(object data);
        List<DTO.MetadataDto> GetMetadataFile();
        bool UpdateMetadataFile(object data);
    }
}
