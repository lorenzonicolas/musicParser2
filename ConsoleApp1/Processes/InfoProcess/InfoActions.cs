using musicParser.DTO;
using musicParser.GoogleDrive;
using musicParser.Metadata;
using musicParser.Utils.FileSystemUtils;
using musicParser.Utils.Loggers;
using musicParser.Utils.Regex;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;

namespace musicParser.Processes.InfoProcess
{
    public class InfoActions : IInfoActions
    {
        private readonly IGoogleDriveService GoogleService;
        private readonly IMetadataService MetadataService;
        private readonly IFileSystemUtils FileSystemUtils;
        private readonly IConsoleLogger ConsoleLogger;
        private readonly IRegexUtils RegexUtils;
        private readonly IFileSystem FS;

        private List<AlbumInfoBackupDto> backupedData;

        public InfoActions(
            IGoogleDriveService googleDrive,
            IMetadataService metadataService,
            IFileSystemUtils fileSystemUtils,
            IConsoleLogger consoleLogger,
            IRegexUtils regexUtils,
            IFileSystem fs)
        {
            GoogleService = googleDrive;
            MetadataService = metadataService;
            FileSystemUtils = fileSystemUtils;
            ConsoleLogger = consoleLogger;
            RegexUtils = regexUtils;
            FS = fs;

            backupedData = GoogleService.GetBackupFile();
        }

        public void Sync(string folderPath, bool updateDeletes = false)
        {
            var folder = FS.DirectoryInfo.FromDirectoryName(folderPath);
            var allAlbums = new List<AlbumInfoOnDisk>();

            if (FileSystemUtils.IsRootArtistsFolder(folder))
            {
                allAlbums.AddRange(GetBandAlbumsInformationFromRoot_Parallel(folder));
            }
            else if (FileSystemUtils.IsArtistFolder(folder))
            {
                allAlbums.AddRange(GetBandAlbumsInformation_Parallel(folder));
            }
            else if (FileSystemUtils.IsAlbumFolder(folder))
            {
                allAlbums.Add(CreateAlbumInfoObj(bandFolder: folder.Parent, albumFolder: folder));
            }

            CheckAllAlbumsInformation(updateDeletes, allAlbums);
        }

        private void CheckAllAlbumsInformation(bool updateDeletes, List<AlbumInfoOnDisk> albumsToUpload)
        {
            // Metadata diff
            var metadataUpdateFileNeeded = MetadataService.SyncMetadataFile(albumsToUpload);

            // Backup diff
            var backupUpdateFileNeeded = false;

            if (updateDeletes)
            {
                backupUpdateFileNeeded |= CheckDeletedAlbums(albumsToUpload);
            }

            backupUpdateFileNeeded |= CheckOutdatedAlbums(albumsToUpload);
            backupUpdateFileNeeded |= CheckNewAlbums(albumsToUpload);

            // Update files to drive
            if(backupUpdateFileNeeded)
            {
                Console.WriteLine("\nUploading backup file to Google Drive...\n");

                var successUpload = GoogleService.UpdateBackupFile(backupedData);

                if (!successUpload)
                {
                    ConsoleLogger.Log("Something went wrong... I didn't uploaded the file to Google Drive!", LogType.Error);
                }
            }

            if (metadataUpdateFileNeeded)
            {
                Console.WriteLine("Uploading metadata file to Google Drive...\n");
                var successUpload = MetadataService.UpdateMetadataFile();

                if (!successUpload)
                {
                    ConsoleLogger.Log("Something went wrong... I didn't uploaded the file to Google Drive!", LogType.Error);
                }
            }
        }

        private List<AlbumInfoOnDisk> GetBandAlbumsInformationFromRoot(IDirectoryInfo rootFolder)
        {
            var list = new List<AlbumInfoOnDisk>();

            foreach (var band in FileSystemUtils.GetFolderArtists(rootFolder))
            {
                list.AddRange(GetBandAlbumsInformation(band));
            }

            return list;
        }

        private List<AlbumInfoOnDisk> GetBandAlbumsInformationFromRoot_Parallel(IDirectoryInfo rootFolder)
        {
            var list = new ConcurrentBag<AlbumInfoOnDisk>();
            var bands = FileSystemUtils.GetFolderArtists(rootFolder);

            Parallel.ForEach(bands, (band) =>
            {
                Console.WriteLine("\nReading band: \"{0}\"", band.Name);
                var results = GetBandAlbumsInformation_Parallel(band);
                foreach (var result in results)
                {
                    list.Add(result);
                }
            });

            return list.ToList();
        }

        private List<AlbumInfoOnDisk> GetBandAlbumsInformation_Parallel(IDirectoryInfo bandFolder)
        {
            var list = new ConcurrentBag<AlbumInfoOnDisk>();
            var albumFolders = FileSystemUtils.GetFolderAlbums(bandFolder);

            Parallel.ForEach(albumFolders, (album) =>
            {
                Console.Write("\tReading album: \"{0}\" from \"{1}\"\n", album.Name, bandFolder.Name);
                var newObj = CreateAlbumInfoObj(bandFolder, album);
                list.Add(newObj);
            });

            return list.ToList();
        }

        private List<AlbumInfoOnDisk> GetBandAlbumsInformation(IDirectoryInfo bandFolder)
        {
            Console.WriteLine("\nReading band: \"{0}\"", bandFolder.Name);
            var list = new List<AlbumInfoOnDisk>();

            foreach (var albumFolder in FileSystemUtils.GetFolderAlbums(bandFolder))
            {
                Console.Write("\tReading album: \"{0}\"\n", albumFolder.Name);
                var newObj = CreateAlbumInfoObj(bandFolder, albumFolder);
                list.Add(newObj);
            }

            return list;
        }

        private AlbumInfoOnDisk CreateAlbumInfoObj(IDirectoryInfo bandFolder, IDirectoryInfo albumFolder)
        {
            var albumInfo = RegexUtils.GetFolderInformation(albumFolder.Name);
            var newObj = new AlbumInfoOnDisk()
            {
                Band = bandFolder.Name,
                AlbumName = albumInfo.Album,
                LastTimeWrite = albumFolder.LastWriteTime,
                FolderPath = albumFolder.FullName,
                Year = albumInfo.Year,
                Name = albumFolder.Name
            };
            return newObj;
        }

        /// <summary>
        ///     Add to the backupedData the new albums not found on Drive
        /// </summary>
        /// <param name="backupedData"></param>
        /// <param name="allAlbums"></param>
        private bool CheckNewAlbums(List<AlbumInfoOnDisk> allAlbums)
        {
            var updateNeeded = false;
            var newAlbums = allAlbums.Where(x => !backupedData.Exists(m => m.AlbumName == x.AlbumName)).ToList();

            if (newAlbums.Count > 0)
            {
                updateNeeded = true;
                Console.WriteLine("\nNew albums:\n");
                foreach (var newAlbum in newAlbums)
                {
                    // TODO get it from the metadata
                    var thisCountry = MetadataService.GetBandCountry(newAlbum.Band);
                    var thisGenre = MetadataService.GetBandGenre(newAlbum.Band);

                    var newRow = new AlbumInfoBackupDto()
                    {
                        Band = newAlbum.Band,
                        AlbumName = newAlbum.AlbumName,
                        Year = newAlbum.Year,
                        Country = thisCountry,
                        Genre = thisGenre,
                        Type = FileSystemUtils.GetAlbumType(newAlbum.Name),
                        DateBackup = DateTime.Now
                    };

                    backupedData.Add(newRow);

                    Console.WriteLine("\t{0} - {1}", newAlbum.Band, newAlbum.AlbumName);
                }
            }
            else
            {
                Console.WriteLine("No new albums");
            }

            backupedData = backupedData.OrderBy(x => x.Band).ThenBy(x => x.Year).ToList();

            return updateNeeded;
        }

        /// <summary>
        /// Add to the backupedData the outdated albums
        /// </summary>
        /// <param name="backupedData"></param>
        /// <param name="allAlbums"></param>
        private bool CheckOutdatedAlbums(List<AlbumInfoOnDisk> allAlbums)
        {
            var updateNeeded = false;
            var outOfDateAlbums = allAlbums.Where(x => backupedData.Exists(m => m.AlbumName == x.AlbumName && m.DateBackup < x.LastTimeWrite)).ToList();

            if (outOfDateAlbums.Count > 0)
            {
                updateNeeded = true;
                Console.WriteLine("\nUpdated albums:\n");
                foreach (var outdated in outOfDateAlbums)
                {
                    var index = backupedData.FindIndex(x => x.AlbumName == outdated.AlbumName);

                    var albumInDisk = allAlbums.Find(x => x.AlbumName == outdated.AlbumName);

                    backupedData[index].Type = FileSystemUtils.GetAlbumType(albumInDisk.Name);
                    backupedData[index].DateBackup = DateTime.Now;

                    Console.WriteLine("\t{0} - {1}", outdated.Band, outdated.AlbumName);
                }
            }
            else
            {
                Console.WriteLine("No albums outdated");
            }

            return updateNeeded;
        }

        /// <summary>
        /// Removes from backupedData the deleted albums on the directory
        /// </summary>
        /// <param name="backupedData"></param>
        /// <param name="allAlbums"></param>
        private bool CheckDeletedAlbums(List<AlbumInfoOnDisk> allAlbums)
        {
            var updateNeeded = false;
            var deletedAlbums = backupedData.Where(x => !allAlbums.Exists(n => n.AlbumName == x.AlbumName)).ToList();

            if (deletedAlbums.Count > 0)
            {
                updateNeeded = true;
                Console.WriteLine("\nDeleted albums:\n");

                foreach (var deleted in deletedAlbums)
                {
                    backupedData.Remove(deleted);

                    Console.WriteLine("\t{0} - {1}", deleted.Band, deleted.AlbumName);
                }
            }
            else
            {
                Console.WriteLine("No albums deleted");
            }

            return updateNeeded;
        }

        /// <summary>
        /// Console print the information gathered.
        /// </summary>
        /// <param name="bands"></param>
        private void PrintInformation(IList<Band> bands)
        {
            var albumCount = 0;
            var genres = new List<string>();
            var years = new List<int>();
            var decades = new List<string>();

            foreach (var band in bands)
            {
                albumCount += band.Albums.Count;

                foreach (var album in band.Albums)
                {
                    genres.Add(album.Genre);
                    years.Add(album.Year);
                    var decade = album.Year % 100 / 10 * 10;
                    decades.Add(decade.ToString());
                }
            }

            var filteredGenres = genres.Select(x => x)
                .GroupBy(s => s)
                .Select(g => new { Name = g.Key, Count = g.Count() });

            var filteredDecades = decades.Select(x => x)
                .GroupBy(s => s)
                .Select(g => new { Name = g.Key, Count = g.Count() });

            Console.WriteLine("\n\nAlbums found: {0}", albumCount);

            Console.WriteLine("\nGenres found:", albumCount);
            foreach (var genre in filteredGenres)
            {
                Console.WriteLine("\t{0}: {1}", genre.Name, genre.Count);
            }

            Console.WriteLine("\nDecades:", albumCount);
            foreach (var decade in filteredDecades)
            {
                Console.WriteLine("\t{0}: {1}", decade.Name, decade.Count);
            }
        }
    }
}
