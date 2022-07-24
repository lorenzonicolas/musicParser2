using Google.Apis.Auth.OAuth2;
using Google.Apis.Download;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;

namespace musicParser.GoogleDrive
{
    public class GoogleDriveAPI : IGoogleDriveAPI
    {
        // If modifying these scopes, delete your previously saved credentials
        // at ~/.credentials/drive-dotnet-quickstart.json
        static readonly string[] Scopes = { DriveService.Scope.DriveFile };
        static readonly string ApplicationName = "Drive API .NET Quickstart";
        static readonly string MymeType = "text/plain";
        static DriveService _service;

        public GoogleDriveAPI()
        {
            _service = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = GetCredentials(),
                ApplicationName = ApplicationName
            });
        }

        /// <summary>
        ///     List all files this program has permission to read
        /// </summary>
        /// <returns></returns>
        public IList<Google.Apis.Drive.v3.Data.File> ListFiles()
        {
            // Define parameters of request
            FilesResource.ListRequest listRequest = _service.Files.List();
            listRequest.PageSize = 10;
            listRequest.Fields = "nextPageToken, files(id, name)";

            // List files
            IList<Google.Apis.Drive.v3.Data.File> files = listRequest.Execute().Files;

            return files;
        }

        /// <summary>
        ///     List files with the given name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public IList<Google.Apis.Drive.v3.Data.File> ListFiles(string name)
        {
            // Define parameters of request
            FilesResource.ListRequest request = _service.Files.List();
            request.PageSize = 10;
            request.Fields = "nextPageToken, files(id, name)";
            request.Q = string.Format("name = '{0}' and trashed = false", name);

            // List files
            IList<Google.Apis.Drive.v3.Data.File> files = request.Execute().Files;
            return files;
        }

        /// <summary>
        ///     Downloads the file as a string
        /// </summary>
        /// <param name="fileId"></param>
        /// <returns></returns>
        public string DownloadFile(string fileId)
        {
            Console.Write($"\nTrying to download file `{fileId}` from Google Drive...\t");

            var request = _service.Files.Get(fileId);

            string output;

            using (var stream = new MemoryStream())
            {
                // Add a handler which will be notified on progress changes.
                // It will notify on each chunk download and when the
                // download is completed or failed.

                request.MediaDownloader.ProgressChanged +=
                        (IDownloadProgress progress) =>
                        {
                            switch (progress.Status)
                            {
                                case DownloadStatus.Downloading:
                                    {
                                        Console.WriteLine(progress.BytesDownloaded);
                                        break;
                                    }
                                case DownloadStatus.Completed:
                                    {
                                        Console.WriteLine("Download complete.\n");
                                        break;
                                    }
                                case DownloadStatus.Failed:
                                    {
                                        Console.WriteLine("Download failed.\n");
                                        break;
                                    }
                            }
                        };
                request.DownloadWithStatus(stream);

                //Reset stream
                stream.Seek(0, SeekOrigin.Begin);

                StreamReader reader = new(stream);
                output = reader.ReadToEnd();
            }

            return output;
        }

        public string GetFileIdFromName(string name)
        {
            try
            {
                var file = ListFiles()
                    .Where(x => x.Name == name)
                    .FirstOrDefault();

                if(file == null)
                {
                    throw new Exception("File not found");
                }

                return file.Id;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Error trying to get the file id. \"{0}\"", ex.Message));
            }
        }

        public void UploadNewFile(string sourcePath, string? saveAs = null)
        {
            var name = string.IsNullOrEmpty(saveAs) ? Path.GetFileName(sourcePath) : saveAs;

            var fileMetadata = new Google.Apis.Drive.v3.Data.File()
            {
                Name = name
            };

            FilesResource.CreateMediaUpload request;

            using (var stream = new FileStream(sourcePath, FileMode.Open))
            {
                request = _service.Files.Create(fileMetadata, stream, MymeType);
                request.Fields = "id";
                request.Upload();
            }

            var response = request.ResponseBody;
            Console.WriteLine("File ID: " + response.Id);
        }

        /// <summary>
        /// Permanently delete a file, skipping the trash.
        /// </summary>
        /// <param name="fileId">ID of the file to delete.</param>
        public void DeleteFile(string fileId)
        {
            try
            {
                _service.Files.Delete(fileId).Execute();
            }
            catch (Exception e)
            {
                Console.WriteLine("An error occurred: " + e.Message);
            }
        }

        public bool UpdateFile(string fileId, string newFilePath)
        {
            try
            {
                using var stream = new FileStream(newFilePath, FileMode.Open);

                // Send the request to the API.
                FilesResource.UpdateMediaUpload request = _service.Files.Update(
                    new Google.Apis.Drive.v3.Data.File(), fileId, stream, MymeType);

                request.Upload();

                return request.ResponseBody != null;
            }
            catch (Exception e)
            {
                Console.WriteLine("An error occurred: " + e.Message);
                return false;
            }
        }

        private static UserCredential GetCredentials()
        {
            UserCredential credential;

            using (var stream =
                new FileStream("client_secret.json", FileMode.Open, FileAccess.Read))
            {
                string credPath = Environment.GetFolderPath(
                    Environment.SpecialFolder.Personal);
                credPath = Path.Combine(credPath, ".credentials/drive-dotnet-quickstart.json");

                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.FromStream(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
                Console.WriteLine("Credential file saved to: " + credPath);
            }

            return credential;
        }
    }
}