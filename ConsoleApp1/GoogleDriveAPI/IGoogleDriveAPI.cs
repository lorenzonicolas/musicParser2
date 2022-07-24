namespace musicParser.GoogleDrive
{
    public interface IGoogleDriveAPI
    {
        IList<Google.Apis.Drive.v3.Data.File> ListFiles();
        IList<Google.Apis.Drive.v3.Data.File> ListFiles(string name);
        string DownloadFile(string fileId);
        string GetFileIdFromName(string name);
        void UploadNewFile(string sourcePath, string? saveAs = null);
        void DeleteFile(string fileId);
        bool UpdateFile(string fileId, string newFilePath);
    }
}
