using Microsoft.Extensions.Configuration;
using musicParser.DTO;
using musicParser.MetalArchives;
using musicParser.Spotify;
using musicParser.TagProcess;
using musicParser.Utils.FileSystemUtils;
using musicParser.Utils.Loggers;
using musicParser.Utils.Regex;
using System.IO.Abstractions;

namespace musicParser.Processes.FilesProcess
{
    public interface IRenameFoldersProcess : IProcess { }
    public class RenameFoldersProcess : IRenameFoldersProcess
    {
        private readonly string MANUAL_FIX_DIR;
        private readonly string WORKING_DIR;

        private readonly IMetalArchivesService metalArchivesService;
        private readonly IExecutionLogger _logger;
        private readonly ISpotifyService spotifyAPI;
        private readonly IFileSystemUtils FileSystemUtils;
        private readonly IRegexUtils RegexUtils;
        private readonly IConsoleLogger ConsoleLogger;
        private readonly ITagsUtils TagsUtils;
        private readonly IFileSystem FS;

        public RenameFoldersProcess (
            IMetalArchivesService maService,
            IExecutionLogger logger,
            ISpotifyService spotify,
            IFileSystemUtils fileSystemUtils,
            IRegexUtils regexUtils,
            IConsoleLogger consoleLogger,
            ITagsUtils tagsUtils,
            IFileSystem fs,
            IConfiguration config)
        {
            metalArchivesService = maService;
            _logger = logger;
            spotifyAPI = spotify;
            FileSystemUtils = fileSystemUtils;
            RegexUtils = regexUtils;
            ConsoleLogger = consoleLogger;
            TagsUtils = tagsUtils;
            FS = fs;

            MANUAL_FIX_DIR = config.GetValue<string>("manual_fix_dir");
            WORKING_DIR = config.GetValue<string>("working_dir");
        }

        /// <summary>
        /// This process will try to format the album folder to specific format.<br/>
        /// Will try to retrieve the year and band name if needed. <br/>
        /// It may move the folder to MANUAL_FIX.
        /// </summary>
        /// <param name="folderToProcess"></param>
        /// <returns>The folder directory path after trying to parse the files</returns>
        public object Execute(string folderToProcess)
        {
            var folderOutput = string.Empty;

            try
            {
                // Move the raw folder to the working directory first
                Log($"Moving '{folderToProcess}' to processing dir ({WORKING_DIR})");
                _logger.Log($"Moving '{folderToProcess}' to processing dir ({WORKING_DIR})");

                var newPath = FileSystemUtils.CopyFolder(folderToProcess, WORKING_DIR);
                var folder = FS.DirectoryInfo.FromDirectoryName(newPath);

                var folderType = FileSystemUtils.GetFolderType(folder);
                switch (folderType)
                {
                    case FolderType.Album:
                        folderOutput = ProcessAlbum(folder);
                        break;

                    //TODO
                    case FolderType.AlbumWithMultipleCDs:
                        //ProcessAlbumWithCDsFolder(folder);
                        break;

                    case FolderType.ArtistWithAlbums:
                        folderOutput = ProcessArtistFolder(folder);
                        break;

                    default:
                        throw new Exception("Invalid folder type!");
                }
            }
            catch (Exception ex)
            {
                Log($"RenameFolder Process Error. Folder: {folderToProcess} \nErr msg: {ex.Message}", LogType.Error);
                _logger.LogError($"RenameFolder Process Error. Folder: {folderToProcess} \nErr msg: {ex.Message}");
                throw;
            }

            return folderOutput;
        }

        /// <summary>
        /// Process the folder as an artist. Will try to create the appropiate folder name.
        /// Will try to get the year, the band name and album name from tags or external services.
        /// It may move the folder to MANUAL_FIX.
        /// </summary>
        /// <param name="artistFolder"></param>
        /// <returns>The updated album path.</returns>
        private string ProcessArtistFolder(IDirectoryInfo artistFolder)
        {
            var artistName = artistFolder.Name;
            var movedToManual = true;

            foreach (var subFolder in FileSystemUtils.GetFolderAlbums(artistFolder))
            {
                var result = ProcessAlbum(subFolder, artistFolder.Name);
                movedToManual &= result.Contains(MANUAL_FIX_DIR);
            }

            return movedToManual ? $"{MANUAL_FIX_DIR}\\{artistFolder.Name}" : $"{WORKING_DIR}\\{artistName}";
        }

        /// <summary>
        /// Process the folder as an album. Will try to create the appropiate folder name.
        /// Will try to get the year, the band name and album name from tags or external services.
        /// It may move the folder to MANUAL_FIX.
        /// </summary>
        /// <param name="folder"></param>
        /// <param name="parentBand"></param>
        /// <returns>The updated album path.</returns>
        private string ProcessAlbum(IDirectoryInfo folder, string? parentBand = null)
        {
            var folderInfo = RegexUtils.GetFolderInformation(folder.Name);

            if (
                CheckYear(folder, folderInfo) &&
                CheckBandName(folder, parentBand, folderInfo) &&
                CheckAlbumName(folder, folderInfo)
            )
            {
                // Success. Move to next step in queue: working files
                
                // Rename the folder to the expected format if needed. i.e. 2020 - AlbumName
                var newPath = RenameFolder(folder, folderInfo);
                folder = FS.DirectoryInfo.FromDirectoryName(newPath);

                // If it's only an album folder, let's create the band name folder
                if (parentBand == null && folderInfo.Band != null)
                {
                    return RegenerateBandName(folder, folderInfo.Band);
                }

                return newPath;
            }
            else
            {
                // Something handleable went wrong. Move to manual fix queue.
                Log("Moved to MANUAL_FIX_DIR from RenameFoldersProcess", LogType.Error);
                _logger.Log("\tMoving to Manual Fix queue from RenameFoldersProcess");
                return FileSystemUtils.MoveFolder(folder.FullName, MANUAL_FIX_DIR);
            }
        }

        /// <summary>
        /// Creates a father-level folder with the band name if it wasn't found.
        /// </summary>
        /// <param name="albumDirectory"></param>
        /// <param name="bandName"></param>
        /// <returns>The new folder path</returns>
        /// <exception cref="ArgumentNullException"></exception>
        private string RegenerateBandName(IDirectoryInfo albumDirectory, string bandName)
        {
            if (bandName == null) throw new ArgumentNullException(nameof(bandName));

            var bandFullDirectoryName = $"{WORKING_DIR}\\{bandName}";

            if (Directory.Exists(bandFullDirectoryName))
            {
                if (albumDirectory.Parent.Name == bandName)
                {
                    return albumDirectory.FullName;
                }
            }
            else
            {
                Directory.CreateDirectory(bandFullDirectoryName);
            }

            var destiny = $"{bandFullDirectoryName}\\{albumDirectory.Name}";
            Directory.Move(albumDirectory.FullName, destiny);
                
            return destiny;
        }

        /// <summary>
        /// Tries to get the album name and set it to the folderInfo. Will try to get it from tags only.
        /// </summary>
        /// <param name="folder"></param>
        /// <param name="folderInfo"></param>
        /// <returns>True if the album name was found</returns>
        private bool CheckAlbumName(IDirectoryInfo folder, FolderInfo folderInfo)
        {
            var found = false;

            if (string.IsNullOrEmpty(folderInfo.Album))
            {
                Log("\tFolder album name not found, will try to get from tags");
                _logger.Log("\tFolder album name not found, will try to get from tags");

                string? album = TagsUtils.GetAlbumFromTag(folder, _logger);
                if (!string.IsNullOrEmpty(album))
                {
                    Log($"Retrieved Album from tag: {album}");
                    _logger.Log($"Retrieved Album from tag: {album}");
                    folderInfo.Album = album;
                }
                else
                {
                    Log($"Couldn't find this folder album name: \"{folder.Name}\"", LogType.Information);
                    _logger.LogError($"Couldn't find this folder album name: \"{folder.Name}\"");
                }
            }
            else
                found = true;

            return found;
        }

        /// <summary>
        /// Tries to get the album band name and set it to the folderInfo. Will try to get it from tags only.
        /// </summary>
        /// <param name="folder"></param>
        /// <param name="folderInfo"></param>
        /// <param name="parentBand"></param>
        /// <returns>True if the band name was found</returns>
        private bool CheckBandName(IDirectoryInfo folder, string? parentBand, FolderInfo folderInfo)
        {
            var found = true;
            if (string.IsNullOrEmpty(folderInfo.Band))
            {
                if (!string.IsNullOrEmpty(parentBand))
                {
                    folderInfo.Band = parentBand;
                    found = true;
                }
                else
                {
                    Log("\tFolder band name not found, will try to get from tags");
                    _logger.Log("\tFolder band name not found, will try to get from tags");
                    string band = TagsUtils.GetArtistFromTag(folder, _logger);
                    if (!string.IsNullOrEmpty(band))
                    {
                        Log($"Retrieved Artist from tag: {band}");
                        _logger.Log($"Retrieved Artist from tag: {band}");
                        folderInfo.Band = band;
                        found = true;
                    }
                    else
                    {
                        Log($"Couldn't find this folder artist name: \"{folder.Name}\"");
                        _logger.LogError($"Couldn't find this folder artist name: \"{folder.Name}\"");
                    }
                }
            }

            return found;
        }

        /// <summary>
        /// Tries to get the album year and set it to the folderInfo. Will try to get it
        /// from tags, Spotify or MetalArchives.
        /// </summary>
        /// <param name="folder"></param>
        /// <param name="folderInfo"></param>
        /// <returns>True if the album year was found</returns>
        private bool CheckYear(IDirectoryInfo folder, FolderInfo folderInfo)
        {
            var found = true;

            if (folderInfo == null)
            {
                folderInfo = new FolderInfo();
            }

            if (string.IsNullOrEmpty(folderInfo.Year))
            {
                Log("\tFolder year not found, will try to get from tags");
                _logger.Log("\tFolder year not found, will try to get from tags");

                var year = TagsUtils.GetYear(folder, _logger);

                if (year.HasValue && TagsUtils.IsValidYear(year.Value))
                {
                    Log($"Retrieved year from tag: {year.Value}");
                    _logger.Log($"Retrieved year from tag: {year.Value}");
                    folderInfo.Year = year.Value.ToString();
                    found = true;
                }
                else
                {
                    var yearFromSpotify = spotifyAPI.GetAlbumYear(folderInfo.Album, folderInfo.Band);
                    if (TagsUtils.IsValidYear(yearFromSpotify))
                    {
                        Log($"Retrieved year from Spotify: {yearFromSpotify}");
                        _logger.Log($"Retrieved year from Spotify: {yearFromSpotify}");
#pragma warning disable CS8601 // Possible null reference assignment.
                        folderInfo.Year = yearFromSpotify;
#pragma warning restore CS8601 // Possible null reference assignment.
                        found = true;
                    }
                    else
                    {
                        var yearFromMetalArchives = metalArchivesService.GetAlbumYearAsync(folderInfo.Band, folderInfo.Album).Result;

                        if (TagsUtils.IsValidYear(yearFromMetalArchives))
                        {
                            Log($"Retrieved year from MetalArchives: {yearFromMetalArchives}");
                            _logger.Log($"Retrieved year from MetalArchives: {yearFromMetalArchives}");
#pragma warning disable CS8601 // Possible null reference assignment.
                            folderInfo.Year = yearFromMetalArchives;
#pragma warning restore CS8601 // Possible null reference assignment.
                            found = true;
                        }
                        else
                        {
                            Log($"Couldn't find this album year: \"{folder.Name}\"");
                            _logger.LogError($"Couldn't find this album year: \"{folder.Name}\"");
                        }
                    }
                }
            }

            return found;
        }

        /// <summary>
        /// Rename the folder to the specific format.
        /// </summary>
        /// <param name="folder"></param>
        /// <param name="folderInfo"></param>
        /// <returns>The new folder path</returns>
        /// <exception cref="ApplicationException"></exception>
        private string RenameFolder(IDirectoryInfo folder, FolderInfo folderInfo)
        {
            if (folderInfo == null || folderInfo.Album == null || folderInfo.Band == null || folderInfo.Year == null)
            {
                throw new ApplicationException("Invalid arguments");
            }

            try
            {
                var expectedFormatFolderName = $"{folderInfo.Year} - {folderInfo.Album}";

                if (folder.Name != expectedFormatFolderName)
                {
                    Log($"Renaming folder to {expectedFormatFolderName}");
                    _logger.Log($"Renaming folder to {expectedFormatFolderName}");
                    Microsoft.VisualBasic.FileIO.FileSystem.RenameDirectory(folder.FullName, expectedFormatFolderName);
                }

                return $"{folder.Parent.FullName}\\{expectedFormatFolderName}";
            }
            catch (Exception)
            {
                Log($"Something went terribly wrong trying to Rename Folder" +
                    $"\nFolder: {folder.FullName}\nFolderInfo: {folderInfo}", LogType.Error);
                _logger.LogError($"Something went terribly wrong trying to Rename Folder" +
                    $"\nFolder: {folder.FullName}\nFolderInfo: {folderInfo}");
                throw;
            }
        }

        private void Log (string message, LogType logType = LogType.Process)
        {
            string header = "FolderProcessor";
            ConsoleLogger.Log($"\t{header} - {message}", logType);
        }
    }
}