using musicParser.DTO;
using musicParser.Utils.Loggers;
using musicParser.Utils.Regex;
using System.IO.Abstractions;

namespace musicParser.Utils.FileSystemUtils
{
    public class FileSystemUtils : IFileSystemUtils
    {
        public static readonly List<string> CoverImageNames = new List<string> { "FRONT.JPG", "FOLDER.JPG" };
        static readonly List<string> ImageExtensions = new List<string> { ".jpg", ".jpe", ".bmp", ".png" };
        static readonly List<string> SongExtensions = new List<string> { ".mp3", ".wav", ".m4a" };

        public IConsoleLogger ConsoleLogger { get; }
        public IRegexUtils RegexUtils { get; }
        public IFileSystem FS { get; }

        public FileSystemUtils(
            IConsoleLogger logger,
            IRegexUtils regexUtils,
            IFileSystem fileSystem)
        {
            ConsoleLogger = logger;
            RegexUtils = regexUtils;
            FS = fileSystem;
        }

        public bool ValidateDirectory(string directory, bool createIfNotExists = false)
        {
            try
            {
                if(createIfNotExists)
                {
                    FS.Directory.CreateDirectory(directory);
                }

                return FS.Directory.Exists(directory);
            }
            catch (Exception ex)
            {
                ConsoleLogger.Log(ex.Message, LogType.Error);
                return false;
            }
        }

        /// <summary>
        /// Gets the cover image of the album if there's any with an specific name ("FRONT.jpg")
        /// </summary>
        /// <param name="cdFolder"></param>
        /// <returns></returns>
        public IFileInfo? GetAlbumCover(IDirectoryInfo cdFolder, IExecutionLogger logger)
        {
            try
            {
                if (!IsAlbumFolder(cdFolder))
                {
                    return null;
                }

                var folderImages = GetFolderImages(cdFolder);

                var alreadyCreatedCover = folderImages
                    .FirstOrDefault(x => CoverImageNames.Contains(x.Name.ToUpper()));

                if (alreadyCreatedCover != null)
                    return alreadyCreatedCover;
                
                //Discard the system files (those albumart small shit)
                folderImages = folderImages.Where(x => !x.Attributes.HasFlag(System.IO.FileAttributes.System)).ToArray();

                if (folderImages.Length == 1)
                {
                    return folderImages.First();
                }
            }
            catch (Exception ex)
            {
                var exMessage = $"Something went wrong trying to retrieve cover image on {cdFolder.FullName}.\n{ex.Message}";
                ConsoleLogger.Log(exMessage, LogType.Information);
                logger.Log(exMessage);
                return null;
            }

            return null;
        }

        public string GetAlbumType(string name)
        {
            if (name.Contains("(EP)") || name.Contains("[EP]"))
                return nameof(AlbumType.EP);

            if (name.Contains("(Demo)") || name.Contains("[Demo]"))
                return nameof(AlbumType.Demo);

            if (name.Contains("(Single)") || name.Contains("[Single]"))
                return nameof(AlbumType.Single);

            if (name.Contains("(Split)") || name.Contains("[Split]"))
                return nameof(AlbumType.Split);

            return nameof(AlbumType.FullAlbum);
        }

        /// <summary>
        /// Determines if the folder contains artist (i.e. "Music\Iron Maiden").
        /// </summary>
        /// <param name="folder">Folder to check</param>
        /// <returns></returns>
        public bool IsRootArtistsFolder(IDirectoryInfo folder)
        {
            //It should not have songs on main folder if its a root artists folder
            if (GetFolderSongs(folder).Length > 0)
            {
                return false;
            }

            //It should not have albums on main folder if its a roots artists folder
            if (GetFolderAlbums(folder).Length > 0)
            {
                return false;
            }

            //Iterates through each inner folder of the supposed root artists folder (they should be artists)
            foreach (var artist in folder.EnumerateDirectories())
            {
                if (IsArtistFolder(artist)) return true;
            }

            return false;
        }

        /// <summary>
        /// Determines if the folder belongs to an artist (i.e. "Iron Maiden").
        /// </summary>
        /// <param name="artistFolder">Folder to check</param>
        /// <returns></returns>
        public bool IsArtistFolder(IDirectoryInfo artistFolder)
        {
            //It should not have songs on main folder if its an artist folder
            if (GetFolderSongs(artistFolder).Length > 0) return false;

            //Iterates through each inner folder of the supposed artist folder (they should be albums)
            foreach (var album in artistFolder.EnumerateDirectories())
            {
                if (IsAlbumFolder(album) 
                    && !album.Name.StartsWith("Disc", StringComparison.InvariantCultureIgnoreCase)
                    && !album.Name.StartsWith("CD", StringComparison.InvariantCultureIgnoreCase))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Determines if the folder is an album (i.e. "2015 - Some Album Name")
        /// </summary>
        /// <param name="albumFolder">Folder to check</param>
        /// <returns></returns>
        public bool IsAlbumFolder(IDirectoryInfo albumFolder)
        {
            if (GetFolderSongs(albumFolder).Count() > 0)
            {
                return true;
            }

            return AlbumContainsCDFolders(albumFolder);
        }

        public bool AlbumContainsCDFolders(IDirectoryInfo folder)
        {
            if (folder == null) return false;

            var albumDiscs = folder.GetDirectories().Where(x =>
                x.Name.StartsWith("Disc", StringComparison.InvariantCultureIgnoreCase) ||
                x.Name.StartsWith("CD", StringComparison.InvariantCultureIgnoreCase)
            );

            foreach (var CD in albumDiscs)
            {
                if (GetFolderSongs(CD).Length > 0) return true;
            }

            return false;
        }

        public IFileInfo? GetAnyFolderSong(IDirectoryInfo cdFolder)
        {
            var folder = cdFolder;

            if(AlbumContainsCDFolders(cdFolder))
            {
                folder = cdFolder
                    .GetDirectories()
                    .Where(x => x.Name.StartsWith("CD") || x.Name.StartsWith("Disc"))
                    .First();
            }

            return folder.GetFiles()
                    .Where(x => SongExtensions.Contains(x.Extension.ToLower()))
                    .OrderBy(x=>x.Length)
                    .FirstOrDefault();
        }

        public IFileInfo[] GetFolderSongs(IDirectoryInfo cdFolder)
        {
            return cdFolder.GetFiles().Where(x => SongExtensions.Contains(x.Extension.ToLower())).ToArray();
        }

        public IFileInfo[] GetFolderImages(IDirectoryInfo cdFolder)
        {
            return cdFolder.GetFiles().Where(x => ImageExtensions.Contains(x.Extension.ToLower())).ToArray();
        }

        public IDirectoryInfo[] GetFolderAlbums(IDirectoryInfo mainFolder)
        {
            return mainFolder.GetDirectories()
                .Where(x => IsAlbumFolder(x))
                .ToArray();
        }

        public IList<IDirectoryInfo> GetFolderAlbums(string path)
        {
            return FS.DirectoryInfo.FromDirectoryName(path)
                .GetDirectories()
                .Where(x => IsAlbumFolder(x))
                .ToList();
        }

        public IDirectoryInfo[] GetFolderArtists(IDirectoryInfo mainFolder)
        {
            return mainFolder.GetDirectories()
                .Where(x => IsArtistFolder(x))
                .ToArray();
        }

        public IDirectoryInfo[] GetFolderArtists(string mainFolder)
        {
            return FS.DirectoryInfo.FromDirectoryName(mainFolder)
                .GetDirectories()
                .Where(x => IsArtistFolder(x))
                .ToArray();
        }

        public bool IsFileLocked(string filename)
        {
            var file = FS.FileInfo.FromFileName(filename);

            if (file.IsReadOnly) return true;

            try
            {
                using var stream2 = FS.FileStream.Create(filename, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
            catch (Exception)
            {
                return true;
            }

            return false;
        }

        public void UnlockFile(string filename)
        {
            var file = FS.FileInfo.FromFileName(filename);

            if (file.IsReadOnly)
            {
                file.IsReadOnly = false;
            }
        }

        public string? GetAlbumFolderName(IFileInfo file)
        {
            string? albumFolderName = null;

            // Use parent directory to get album name if it's an inner cd's folder
            if (AlbumContainsCDFolders(file.Directory.Parent))
            {
                albumFolderName = file.Directory.Parent.Name;
            }
            else if (IsAlbumFolder(file.Directory))
            {
                albumFolderName = file.Directory.Name;
            }

            return albumFolderName != null ? RegexUtils.GetFolderInformation(albumFolderName).Album : null;
        }

        public FolderType GetFolderType(IDirectoryInfo folder)
        {
            if (IsArtistFolder(folder))
                return FolderType.ArtistWithAlbums;

            if (AlbumContainsCDFolders(folder))
                return FolderType.AlbumWithMultipleCDs;

            if (IsAlbumFolder(folder))
                return FolderType.Album;
            
            throw new FolderTypeException("Couldn't retrieve folder type for: " + folder.Name);
        }

        public void SaveImageFile(IDirectoryInfo cdFolder, byte[] imageTagBytes)
        {
            var outputCover = FS.Path.Combine(cdFolder.FullName, "FRONT.jpg");
            
            using (var fs = FS.FileStream.Create(outputCover, System.IO.FileMode.CreateNew))
            {
                fs.Write(imageTagBytes, 0, imageTagBytes.Length);
            }
        }

        public string MoveFolder(string source, string destination)
        {
            var lastPart = source.Split('\\').Last();
            var fullDestination = $"{destination}\\{lastPart}";

            if (!FS.Directory.Exists(destination))
            {
                FS.Directory.CreateDirectory(destination);
            }

            FS.DirectoryInfo
                .FromDirectoryName(source)
                .MoveTo(fullDestination);

            return fullDestination;
        }

        public string CopyFolder(string source, string destinationDir)
        {
            var sourceDirectory = FS.DirectoryInfo.FromDirectoryName(source);
            var lastPart = source.Split('\\').Last();
            var destinyFolder = $"{destinationDir}\\{lastPart}";

            if (!FS.Directory.Exists(destinyFolder))
            {
                FS.Directory.CreateDirectory(destinyFolder);
            }

            foreach (var dir in sourceDirectory.GetDirectories())
            {
                string dirToCreate = dir.FullName.Replace(sourceDirectory.FullName, destinyFolder);
                FS.Directory.CreateDirectory(dirToCreate);
            }

            foreach (var file in sourceDirectory.GetFiles())
            {
                FS.File.Copy(file.FullName, file.FullName.Replace(sourceDirectory.FullName, destinyFolder), true);
            }

            return destinyFolder;
        }

        public string MoveProcessedFolder(string sourcePath, string outputPath)
        {
            var dirInfo = FS.DirectoryInfo.FromDirectoryName(sourcePath);
            var parentInfo = dirInfo.Parent;
            string bandName;

            // This should be always the case because it was already reconstructed into the Band\Album tree.
            bool isBandFolder = IsArtistFolder(dirInfo);
            bool isAlbumFolder = IsAlbumFolder(dirInfo);

            if (isAlbumFolder)
            {
                bandName = parentInfo.Name;
            }
            else if (isBandFolder)
            {
                bandName = dirInfo.Name;
            }
            else
            {
                throw new ApplicationException("Couldn't retrieve band's name from folder");
            }

            string fullDestinyPath;

            if (!string.IsNullOrEmpty(bandName))
            {
                var destinyBandPath = $"{outputPath}\\{bandName}";
                fullDestinyPath = isBandFolder ? destinyBandPath : $"{destinyBandPath}\\{dirInfo.Name}";

                MoveFolder(sourcePath, destinyBandPath);

                // Clean the working folder if doesn't contain any other album
                if (isAlbumFolder && parentInfo.GetDirectories().Length < 1)
                {
                    try
                    {
                        FS.Directory.Delete(parentInfo.FullName);
                    }
                    catch (Exception)
                    {
                        // This is not a blocking operation - dont care if this cleanup fails.
                    }
                }
            }
            else
            {
                ConsoleLogger.Log("Didn't find band name - moving folder as it is", LogType.Information);
                fullDestinyPath = $"{outputPath}\\{dirInfo.Name}";
                MoveFolder(sourcePath, fullDestinyPath);
            }

            return fullDestinyPath;
        }

        public bool IsAlbumNameCorrect(string albumName)
        {
            return CoverImageNames.Contains(albumName.ToUpper());
        }

        public class FolderTypeException : FolderProcessException
        {
            public FolderTypeException(string message) : base(message) { }
        }

        public class FolderProcessException : Exception
        {
            public FolderProcessException(string message) : base(message) { }
        }
    }
}
