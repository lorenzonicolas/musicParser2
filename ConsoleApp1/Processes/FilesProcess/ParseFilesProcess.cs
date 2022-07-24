using Microsoft.Extensions.Configuration;
using musicParser.DTO;
using musicParser.MetalArchives;
using musicParser.Spotify;
using musicParser.TagProcess;
using musicParser.Utils.FileSystemUtils;
using musicParser.Utils.Loggers;
using musicParser.Utils.Regex;
using System;
using System.Configuration;
using System.IO;
using System.IO.Abstractions;

namespace musicParser.Processes.FilesProcess
{
    public interface IParseFileProcess : IProcess { }
    public class ParseFileProcess : IParseFileProcess
    {
        private readonly IMetalArchivesService metalArchivesService;
        private readonly IExecutionLogger _logger;
        private readonly ISpotifyService spotifyService;
        private readonly IFileSystemUtils FileSystemUtils;
        private readonly IRegexUtils RegexUtils;
        private readonly IConsoleLogger ConsoleLogger;
        private readonly ITagsUtils TagsUtils;
        private readonly IFileSystem fs;
        private readonly string MANUAL_FIX_DIR;

        public ParseFileProcess(
            IMetalArchivesService service,
            IExecutionLogger logger,
            ISpotifyService spotify,
            IFileSystemUtils fileSystemUtils,
            IRegexUtils regexUtils,
            IConsoleLogger consoleLogger,
            ITagsUtils tagsUtils,
            IFileSystem FS,
            IConfiguration config)
        {
            metalArchivesService = service;
            _logger = logger;
            spotifyService = spotify;
            FileSystemUtils = fileSystemUtils;
            RegexUtils = regexUtils;
            ConsoleLogger = consoleLogger;
            TagsUtils = tagsUtils;
            fs = FS;

            MANUAL_FIX_DIR = config.GetValue<string>("manual_fix_dir");
        }

        /// <summary>
        /// This process will try to format the song files and album cover to specific format.<br/>
        /// It may move the folder to MANUAL_FIX.
        /// </summary>
        /// <param name="folderToProcess"></param>
        /// <returns>The folder directory path after trying to parse the files</returns>
        public object Execute(string folderToProcess)
        {
            var movedToManualQueue = true;
            var folderInfo = fs.DirectoryInfo.FromDirectoryName(folderToProcess);

            if (FileSystemUtils.IsArtistFolder(folderInfo))
            {
                string resultPath = folderToProcess;

                foreach (var cdFolder in folderInfo.GetDirectories())
                {
                    resultPath = ProcessFolderFiles(cdFolder);
                    movedToManualQueue &= resultPath.Contains(MANUAL_FIX_DIR);
                }

                return resultPath;
            }
            else if(FileSystemUtils.IsAlbumFolder(folderInfo))
            {
                return ProcessFolderFiles(folderInfo);
            }

            return folderToProcess;
        }

        /// <summary>
        /// Process the album files: songs and album cover. Tries to format song names and album cover to
        /// the specific format.
        /// </summary>
        /// <param name="cdFolder"></param>
        /// <returns>The same folder full name</returns>
        private string ProcessFolderFiles(IDirectoryInfo cdFolder)
        {
            try
            {
                var songs = FileSystemUtils.GetFolderSongs(cdFolder);

                var folderImageSuccess = ProcessFolderImage(cdFolder, songs);
                
                var songsSuccess = false;

                if (songs.Length >= 1)
                {
                    songsSuccess = ProcessSongs(songs);
                }
                else
                {
                    //There may be "CD1" folders inside this album.
                    foreach (var folder in cdFolder.GetDirectories())
                    {
                        if (FileSystemUtils.IsAlbumFolder(folder))
                        {
                            songs = FileSystemUtils.GetFolderSongs(folder);
                            songsSuccess = ProcessSongs(songs);
                        }
                    }
                }

                if (!folderImageSuccess || !songsSuccess)
                {
                    // Something handleable went wrong. Move to manual fix queue.
                    Log($"Moving to Manual Fix queue from ParseFiles.", LogType.Error);
                    _logger.Log($"Moving to Manual Fix queue from ParseFiles:\n\tfolderImageSuccess: {folderImageSuccess}\n\tsongsSucess: {songsSuccess}");
                    return FileSystemUtils.MoveProcessedFolder(cdFolder.FullName, MANUAL_FIX_DIR);
                }
            }
            catch (Exception ex)
            {
                Log($"ParseFiles Process Error. Folder: {cdFolder.FullName} \nErr msg: {ex.Message}", LogType.Error);
                _logger.LogError($"ParseFiles Process Error. Folder: {cdFolder.FullName} \nErr msg: {ex.Message}");
                throw;
            }

            return cdFolder.FullName;
        }

        /// <summary>
        /// Tries to set the album cover image file as the specified format. If not found in the folder,
        /// will try to get it from metal archives api or Spotify.
        /// </summary>
        /// <param name="cdFolder"></param>
        /// <param name="songs"></param>
        /// <returns>True if the album cover is OK (found, and renamed to specified format)</returns>
        private bool ProcessFolderImage(IDirectoryInfo cdFolder, IFileInfo[] songs)
        {
            try
            {
                var albumCover = FileSystemUtils.GetAlbumCover(cdFolder, _logger);

                if (albumCover != null)
                {
                    if (!FileSystemUtils.IsAlbumNameCorrect(albumCover.Name))
                    {
                        albumCover.MoveTo(string.Format("{0}\\FRONT.jpg", cdFolder.FullName));
                        Log($"Renamed image {albumCover.Name} in folder {cdFolder.Name}");
                        _logger.Log($"Renamed image {albumCover.Name} in folder {cdFolder.Name}");
                    }
                    return true;
                }
                else
                {
                    return TryToGetImage(cdFolder, songs);
                }
            }
            catch (Exception ex)
            {
                Log($"Error processing folder image. Ex: {ex.Message}\nCD: {cdFolder.Name}", LogType.Error);
                _logger.LogError($"Error processing folder image. Ex: {ex.Message}\nCD: {cdFolder.Name}");
                return false;
            }
        }

        /// <summary>
        /// Tries to get the album image from tags, Metalarchives or Spotify
        /// </summary>
        /// <param name="cdFolder"></param>
        /// <param name="songs"></param>
        /// <returns>True if the image was retrieved</returns>
        private bool TryToGetImage(IDirectoryInfo cdFolder, IFileInfo[] songs)
        {
            var found = false;

            //Get from tags
            var imageTagBytes = TagsUtils.GetCover(songs);

            if (imageTagBytes != null)
            {
                _logger.Log($"Retrieved cover image from tags. Folder: {cdFolder.Name}");
                FileSystemUtils.SaveImageFile(cdFolder, imageTagBytes);
                found = true;
            }
            else
            {
                var albumName = RegexUtils.GetFolderInformation(cdFolder.Name).Album;
                var bandName = cdFolder.Parent.Name;
                
                //Get from Spotify
                imageTagBytes = spotifyService.DownloadAlbumCover(bandName, albumName);
                if (imageTagBytes != null)
                {
                    _logger.Log($"Retrieved cover image from Spotify. Folder: {cdFolder.Name}");
                    FileSystemUtils.SaveImageFile(cdFolder, imageTagBytes);
                    found = true;
                }
                else
                {
                    //Get from MetalArchives
                    imageTagBytes = metalArchivesService.DownloadAlbumCover(bandName, albumName).Result;

                    if (imageTagBytes != null)
                    {
                        _logger.Log($"Retrieved cover image from MetalArchives. Folder: {cdFolder.Name}");
                        FileSystemUtils.SaveImageFile(cdFolder, imageTagBytes);
                        found = true;
                    }
                    else
                    {
                        _logger.LogError($"Couldn't retrieve any album image from internet. Folder: {cdFolder.Name}");
                    }
                }
            }

            return found;
        }

        /// <summary>
        /// Renames the song if needed to the specific format
        /// </summary>
        /// <param name="Tracks"></param>
        /// <returns>True if all songs are OK</returns>
        private bool ProcessSongs(IFileInfo[] Tracks)
        {
            var success = true;

            foreach (var file in Tracks)
            {
                try
                {
                    var songInfo = RegexUtils.GetFileInformation(file.Name);
                    var expectedFileName = $"{songInfo.TrackNumber} - {songInfo.Title}.{songInfo.Extension}";

                    if(expectedFileName != file.Name)
                    {
                        RenameSong(songInfo.TrackNumber, songInfo.Title, songInfo.Extension, file);
                    }
                }
                catch (RegexUtils.FileInfoException)
                {
                    var message = $"Couldn't match any valid file name: {file.FullName}. Will try to get it from tags.";
                    Log(message, LogType.Information);
                    _logger.Log(message);

                    var songInfo = TagsUtils.GetFileInformation(file);
                    
                    if(songInfo == null || !int.TryParse(songInfo.TrackNumber, out int result) || result <= 0 || string.IsNullOrEmpty(songInfo.Title))
                    {
                        success = false;
                    }
                    else
                    {
                        var expectedFileName = $"{songInfo.TrackNumber} - {songInfo.Title}.{songInfo.Extension}";

                        if (expectedFileName != file.Name)
                        {
                            RenameSong(songInfo.TrackNumber, songInfo.Title, songInfo.Extension, file);
                        }
                    }

                    continue;
                }
                catch (Exception ex)
                {
                    Log("There was an error trying to rename this song\nFile: " + file.FullName, LogType.Error);
                    _logger.LogError($"Error renaming song file: {file.FullName}\nEx: {ex.Message}");
                    success = false;
                    continue;
                }
            }

            return success;
        }

        public void RenameSong(string trackNumber, string title, string extension, IFileInfo file)
        {            
            var correctFileFormat = string.Format("{0} - {1}.{2}", trackNumber, title, extension);
            var destinationPath = Path.Combine(file.DirectoryName, correctFileFormat);

            Log($"Renaming \"{file.Name}\" to \"{correctFileFormat}\"");

            file.MoveTo(destinationPath);

            _logger.Log($"Renamed song file: {correctFileFormat}");
        }

        private void Log(string message, LogType logType = LogType.Process)
        {
            string header = "FilesProcessor";
            ConsoleLogger.Log($"\t{header} - {message}", logType);
        }
    }
}