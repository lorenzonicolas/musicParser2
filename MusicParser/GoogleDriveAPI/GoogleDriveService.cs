namespace musicParser.GoogleDrive
{
    public class GoogleDriveService : IGoogleDriveService
    {
        private readonly IGoogleDriveAPI _googleDriveAPI;
        public string BackupFilename = "musicParser_backup.txt";
        public string MetadataFilename = "musicParser_metadata.txt";

        public GoogleDriveService(IGoogleDriveAPI googleDriveAPI)
        {
            _googleDriveAPI = googleDriveAPI;
        }

        public void DeleteFile(string id)
        {
            _googleDriveAPI.DeleteFile(id);
        }

        /// <summary>
        ///     Retrieves the backup file uploaded on Drive. It creates a new one if none found.
        /// </summary>
        /// <returns></returns>
        public List<DTO.AlbumInfoBackupDto> GetBackupFile()
        {
            try
            {
                var backupedFile = DownloadFile(BackupFilename);
                if (string.IsNullOrEmpty(backupedFile))
                {
                    throw new Exception("Invalid backup File");
                }

                var result = Newtonsoft.Json.JsonConvert.DeserializeObject<List<DTO.AlbumInfoBackupDto>>(backupedFile);

                if (result == null)
                {
                    throw new Exception("Invalid metadata file");
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Error reading the backup file. \"{0}\"", ex.Message));
            }
        }

        /// <summary>
        ///     Update backup file.
        /// </summary>
        /// <returns></returns>
        public bool UpdateBackupFile(object data)
        {
            return UpdateFile(data, BackupFilename);
        }

        public List<DTO.MetadataDto> GetMetadataFile()
        {
            try
            {
                var metadataFile = DownloadFile(MetadataFilename);
                if(string.IsNullOrEmpty(metadataFile))
                {
                    throw new Exception("Invalid metadata File");
                }

                var result = Newtonsoft.Json.JsonConvert.DeserializeObject<List<DTO.MetadataDto>>(metadataFile);

                if(result == null)
                {
                    throw new Exception("Invalid metadata file");
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Error reading the metadata file. \"{0}\"", ex.Message));
            }
        }

        /// <summary>
        ///     Update metadata file.
        /// </summary>
        /// <returns></returns>
        public bool UpdateMetadataFile(object data)
        {
            return UpdateFile(data, MetadataFilename);
        }

        private bool UpdateFile(object data, string file)
        {
            GenerateOutputFile(data, file);

            var myFileId = GetDriveFileId(file);
            return _googleDriveAPI.UpdateFile(myFileId, Path.Combine(Path.GetTempPath(), file));
        }

        private string GetDriveFileId(string file)
        {
            var fileList = _googleDriveAPI.ListFiles(file);

            if (fileList.Count > 1) throw new Exception(string.Format("More than one file \"{0}\" to download!", MetadataFilename));

            if (fileList.Count < 1)
            {
                Console.WriteLine("No file found! Creating empty template file on Drive...");
                CreateEmptyFile(file);

                //reload it
                fileList = _googleDriveAPI.ListFiles(file);
            }

            return fileList.First().Id;
        }

        private string DownloadFile(string file)
        {
            return _googleDriveAPI.DownloadFile(GetDriveFileId(file));
        }

        /// <summary>
        ///     Creates new backup template on drive
        /// </summary>
        private void CreateEmptyFile(string name)
        {
            //create sample file
            string[] line = { "[]" };

            var destination = Path.Combine(Path.GetTempPath(), name);

            File.WriteAllLines(destination, line);

            _googleDriveAPI.UploadNewFile(destination);
        }

        /// <summary>
        ///     Generates the json output file as txt
        /// </summary>
        /// <param name="fullRows"></param>
        private void GenerateOutputFile(object data, string file)
        {
            var output = Newtonsoft.Json.JsonConvert.SerializeObject(data , Newtonsoft.Json.Formatting.Indented);
            var destination = Path.Combine(Path.GetTempPath(), file);
            File.WriteAllText(destination, output);
        }
    }
}
