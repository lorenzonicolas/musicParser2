using musicParser.DTO;
using musicParser.Metadata;
using musicParser.MetalArchives;
using musicParser.Processes;
using musicParser.Spotify;
using musicParser.Utils.FileSystemUtils;
using musicParser.Utils.Loggers;
using musicParser.Utils.Regex;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;

namespace musicParser.TagProcess
{
    public interface ITagProcess : IProcess
    {
    }

    partial class TagsProcess : ITagProcess
    {
        private readonly IExecutionLogger _logger;
        private readonly IConsoleLogger ConsoleLogger;
        private readonly ITagsUtils TagsUtils;
        private readonly IFileSystem FS;
        private readonly IMetadataService metadataServices;
        private readonly IMetalArchivesService metalArchivesService;
        private readonly ISpotifyService spotifyService;
        private readonly IFileSystemUtils FileSystemUtils;
        private readonly IRegexUtils RegexUtils;

        public TagsProcess(
            IMetadataService metadata, 
            IMetalArchivesService metalArchives, 
            IExecutionLogger logger,
            ISpotifyService spotify,
            IFileSystemUtils fileSystemUtils,
            IRegexUtils regexUtils,
            IConsoleLogger consoleLogger,
            ITagsUtils tagsUtils,
            IFileSystem fs)
        {
            metadataServices = metadata;
            metalArchivesService = metalArchives;
            spotifyService = spotify;
            FileSystemUtils = fileSystemUtils;
            RegexUtils = regexUtils;
            _logger = logger;
            ConsoleLogger = consoleLogger;
            TagsUtils = tagsUtils;
            FS = fs;
        }

        /// <summary>
        /// This process will try to validate every tag within each track. It may rename the file.
        /// <br/>This is the list of actions this method will execute:
        /// <list type="bullet">
        /// <item>Remove comments</item>
        /// <item>Validate track number comparing to file name</item>
        /// <item>Validate track name comparing to file name</item>
        /// <item>Validate album name comparing to folder name</item>
        /// <item>Validate tracks genres comparing to metadata file</item>
        /// </list>
        /// </summary>
        /// <param name="folderToProcess"></param>
        /// <returns>True if needs manual fix (at least one tag is not OK)</returns>
        public object Execute(string folderToProcess)
        {
            var folderInfo = FS.DirectoryInfo.FromDirectoryName(folderToProcess);

            if (FileSystemUtils.IsArtistFolder(folderInfo))
            {
                return ProcessAsArtistFolder(folderInfo);
            }
            else if (FileSystemUtils.IsAlbumFolder(folderInfo))
            {
                return ProcessAsAlbumFolder(folderInfo);
            }
            else
                throw new ApplicationException("Invalid folder type: nor artist nor album folder");
        }

        /// <summary>
        /// Same description as <seealso cref="ProcessAlbumFolder(IDirectoryInfo, FolderInfo, bool)"/>
        /// </summary>
        /// <param name="album"></param>
        /// <returns>True if needs manual fix (at least one tag is not OK)</returns>
        private bool ProcessAsArtistFolder(IDirectoryInfo artist)
        {
            var needManualFix = false;

            foreach (var album in FileSystemUtils.GetFolderAlbums(artist))
            {
                try
                {
                    needManualFix |= ProcessAsAlbumFolder(album);
                }
                catch (Exception)
                {
                    needManualFix = false;
                    continue;
                }
            }

            return needManualFix;
        }

        /// <summary>
        /// Same description as <seealso cref="ProcessAlbumFolder(IDirectoryInfo, FolderInfo, bool)"/>
        /// </summary>
        /// <param name="album"></param>
        /// <returns>True if needs manual fix (at least one tag is not OK)</returns>
        private bool ProcessAsAlbumFolder(IDirectoryInfo album)
        {
            var needManualFix = false;

            try
            {
                var albumInfo = RegexUtils.GetFolderInformation(album.Name);
                albumInfo.Band = album.Parent.Name;

                if (FileSystemUtils.AlbumContainsCDFolders(album))
                {
                    _logger.Log("Album with inner CD folders!");
                    Log("Album with inner CD folders!", LogType.Information);
                    foreach (var cd in FileSystemUtils.GetFolderAlbums(album))
                    {
                        needManualFix |= ProcessAlbumFolder(cd, albumInfo, true);
                    }
                }
                else
                {
                    needManualFix = ProcessAlbumFolder(album, albumInfo, false);
                }

                return needManualFix;
            }
            catch (Exception ex)
            {
                Log($"Tags Process Error. Folder: {album.FullName} \nErr msg: {ex.Message}", LogType.Error);
                _logger.LogError($"Tags Process Error. Folder: {album.FullName} \nErr msg: {ex.Message}");
                throw;
            }
        }

        //private void ProcessAllFolders(DirectoryInfo[] artistsFolders)
        //{
        //    foreach (var artist in artistsFolders)
        //    {
        //        try
        //        {
        //            foreach (var folder in FileSystemUtils.GetFolderAlbums(artist))
        //            {
        //                Log(string.Format("\n - Reading album \"{0}\", from artist \"{1}\" - \n", folder, artist), LogType.Information);

        //                try
        //                {
        //                    var albumInfo = RegexUtils.GetFolderInformation(folder.Name);
        //                    albumInfo.Band = artist.Name;

        //                    if (FileSystemUtils.AlbumContainCDFolders(folder))
        //                    {
        //                        Console.WriteLine("Album with inner CD folders!");
        //                        foreach (var cd in FileSystemUtils.GetFolderAlbums(folder))
        //                        {
        //                            ProcessAlbumFolder(cd, albumInfo, true);
        //                        }
        //                    }
        //                    else
        //                        ProcessAlbumFolder(folder, albumInfo, false);
        //                }
        //                catch (Exception ex)
        //                {
        //                    Log("Error tag processing this folder: " + folder.FullName + " - Err msg: " + ex.Message, LogType.Error);
        //                    continue;
        //                }
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            Log("Error tag processing this artist: " + artist.FullName + " - Err msg: " + ex.Message, LogType.Error);
        //            continue;
        //        }
        //    }
        //}

        /// <summary>
        /// This method will try to validate every tag within each track. It may rename the file.
        /// <br/>This is the list of actions this method will execute:
        /// <list type="bullet">
        /// <item>Remove comments</item>
        /// <item>Validate track number comparing to file name</item>
        /// <item>Validate track name comparing to file name</item>
        /// <item>Validate album name comparing to folder name</item>
        /// <item>Validate tracks genres comparing to metadata file</item>
        /// </list>
        /// </summary>
        /// <param name="folder"></param>
        /// <param name="albumInfo"></param>
        /// <param name="isInnerCD"></param>
        /// <returns>True if needs manual fix (at least one tag is not OK)</returns>
        private bool ProcessAlbumFolder(IDirectoryInfo folder, FolderInfo albumInfo, bool isInnerCD)
        {
            bool needManualFix;

            var albumGenreFromMeta = metadataServices.GetBandGenre(albumInfo.Band);
            var noGenreOnMetadata = string.IsNullOrEmpty(albumGenreFromMeta) || string.Equals(albumGenreFromMeta, "Unknown", StringComparison.InvariantCultureIgnoreCase);
            var genresFound = new List<string>();

            var albumNameAction = AlbumAction.NoChanges;
            var albumNamesFound = new List<string>();

            foreach (var songFile in FileSystemUtils.GetFolderSongs(folder))
            {
                try
                {
                    //In case it's readonly
                    TagsUtils.UnlockFile(songFile.FullName);

                    using var tagFile = TagLib.File.Create(songFile.FullName);
                    var tags = tagFile.Tag;
                    var info = RegexUtils.GetFileInformation(songFile.Name);

                    // File individual actions
                    var commentsAction = GetTrackCommentsAction(tags.Comment);
                    var trackNumberAction = GetTrackNumberAction(tags.Track, Convert.ToInt32(info.TrackNumber));
                    var trackTitleAction = GetTrackTitleAction(tags.Title, info.Title);
                    // TODO
                    //CheckTrackCover();

                    // Album actions
                    if (TagsUtils.NamesAreDifferent(tags.Album, albumInfo.Album))
                    {
                        albumNamesFound.Add(tags.Album);
                        albumNameAction = AlbumAction.UpdateFolder_Name;
                    }

                    var songGenreAction = TagAction.NoChanges;

                    if (noGenreOnMetadata)
                    {
                        // No metadata genre for this band. Lets add all the genres found on the songs to later evaluate them all.
                        genresFound.AddRange(tags.Genres);
                    }
                    else // This band exists on the metadata, auto-correct genre with it
                    {
                        songGenreAction = GetTrackGenreAction(tags.Genres, albumGenreFromMeta);
                    }

                    // First process the tag changes before renaming the file if needed
                    var hasToSave = false;
                    hasToSave |= ProcessTagAction(commentsAction, tags, info);
                    hasToSave |= ProcessTagAction(trackNumberAction, tags, info);
                    hasToSave |= ProcessTagAction(trackTitleAction, tags, info);
                    hasToSave |= ProcessTagAction(songGenreAction, tags, info, albumInfo.Band, albumGenreFromMeta);

                    if (hasToSave)
                    {
                        tagFile.Save();
                    }

                    //Check if a file rename is needed
                    ProcessFileActions(trackNumberAction, trackTitleAction, tags, info, songFile);
                }
                catch (Exception ex)
                {
                    Log($"Error on tag processing file: {songFile.FullName} - Err msg: {ex.Message}", LogType.Error);
                    _logger.LogError($"Error on tag processing file: {songFile.FullName} - Err msg: {ex.Message}");
                    continue;
                }
            }

            // Only process a general album genre change if no metadata was found.
            // If no metadata was found it means that I haven't validated the band genre yet.
            // Otherwhise each song was previously replaced with the meta genre (solved).
            if (noGenreOnMetadata)
            {
                _logger.Log("This band does not exists on metadata yet.");
                Log("This band does not exists on metadata yet.");

                CheckAlbumGenreV2(folder, albumInfo, genresFound.Distinct().ToList());
            }

            var needAlbumManualFix = false;
            if (albumNameAction == AlbumAction.UpdateFolder_Name)
            {
                needAlbumManualFix = CheckAlbumName(folder, albumInfo, albumNamesFound.Distinct().ToList());
            }

            // Need to manually fix this if there was a genre or album tag name discrepancy.
            needManualFix = noGenreOnMetadata || needAlbumManualFix;

            return needManualFix;
        }

        private bool CheckAlbumName(IDirectoryInfo folder, FolderInfo albumInfo, IList<string> albumNamesFound)
        {
            IList<string> differenceBase = new List<string>()
            {
                "(LimitedEdition)",
                "(DeluxeEdition)",
                "(Single)",
                "/",
                "......",
                "...",
                ".",
                "(EP)",
                "[EP]"
            };

            var foundManyAlbumNameTags = albumNamesFound.Count > 1;
            var foundNoAlbumNameTags = albumNamesFound.Count < 1 || (albumNamesFound.Count == 1 && string.IsNullOrEmpty(albumNamesFound[0]));

            if (foundManyAlbumNameTags || foundNoAlbumNameTags)
            {
                TagsUtils.UpdateAlbumTitleTags(folder, albumInfo.Album, _logger);
                return false;
            }

            var solved = true;
            foreach (var albumNameFound in albumNamesFound)
            {
                var primary = albumNameFound.Length > albumInfo.Album.Length ? albumNameFound : albumInfo.Album;
                var secondary = primary == albumInfo.Album ? albumNameFound : albumInfo.Album;

                // Replace every ocurrence in the longest string based on the secondary string
                foreach (var sub in secondary.Split(' '))
                {
                    primary = primary.Replace(sub, "");
                }

                // Check if the remaining differences are already specified to skip in the config string
                if (differenceBase.Contains(primary.Replace(" ", "")))
                {
                    solved &= true;
                }
                else
                {
                    solved = false;
                }
            }

            if (solved)
            {
                Log($"TAG - Found album name discrepancy but it's considered avoidable:" +
                    $"\nFrom folder: {albumInfo.Album}" +
                    $"\nFrom tags: {string.Join(", ", albumNamesFound)}", LogType.Information);
                _logger.LogTagNote($"TAG - Found album name discrepancy but it's considered avoidable:\nFrom folder: {albumInfo.Album}\nFrom tags: {string.Join(", ", albumNamesFound)}");
                return false;
            }
            
            Log($"TAG - Found album name discrepancy, will need manual fix:" +
                $"\nFrom folder: {albumInfo.Album}" +
                $"\nFrom tags: {string.Join(", ", albumNamesFound)}", LogType.Information);
            _logger.LogTagNote($"TAG - Found album name discrepancy:\nFrom folder: {albumInfo.Album}\nFrom tags: {string.Join(", ", albumNamesFound)}");
            return true;
        }

        private void CheckAlbumGenreV2(IDirectoryInfo folder, FolderInfo albumInfo, IList<string> genresFound)
        {
            // Try to get genre from MetalArchives
            var genreFromMetalArchives = metalArchivesService.GetBandGenreAsync(albumInfo.Band, albumInfo.Album);

            var genreFromSpotify = spotifyService.GetArtistGenreUsingAlbum(albumInfo.Band, albumInfo.Album);

            if (genresFound.Count > 1)
            {
                Log($"TAG - Found multiple genres on tags and has no Metadata in Drive.\nTags:{string.Join(", ", genresFound)}");
                _logger.LogTagNote($"TAG - Found multiple genres on tags and has no Metadata in Drive.\nTags:{string.Join(", ", genresFound)}");
            }
            else if (genresFound.Count < 1 || (genresFound.Count == 1 && (string.IsNullOrEmpty(genresFound[0]) || genresFound[0] == " ")))
            {
                Log($"TAG - Found no genre on tags and has no Metadata in Drive\tAlbum {folder.Name}");
                _logger.LogTagNote($"TAG - Found no genre on tags and has no Metadata in Drive\tAlbum {folder.Name}");
            }
            else
            {
                Log($"TAG - Validate Genre\nFound this genre on tags but has no Metadata on Drive: {genresFound[0]}" +
                    $"\nAlbum {folder.Name}");
                _logger.LogTagNote($"TAG - Validate Genre\nFound this genre on tags but has no Metadata on Drive: {genresFound[0]}" +
                    $"\nAlbum {folder.Name}");
            }

            if (!string.IsNullOrEmpty(genreFromMetalArchives.Result))
            {
                Log($"Genres recommendation by MetalArchives: {genreFromMetalArchives.Result}");
                _logger.LogTagNote($"Genres recommendation by MetalArchives: {genreFromMetalArchives.Result}");
            }
            else
            {
                Log("No genres found by MetalArchives");
                _logger.LogTagNote("No genres found by MetalArchives");
            }

            if (genreFromSpotify?.Count > 0)
            {
                Log($"Genres recommendation by Spotify: {string.Join(", ", genreFromSpotify)}\nAlbum {folder.Name}");
                _logger.LogTagNote($"Genres recommendation by Spotify: {string.Join(", ", genreFromSpotify)}\nAlbum {folder.Name}");
            }
            else
            {
                Log("No genres found by Spotify");
                _logger.LogTagNote("No genres found by Spotify");
            }

            _logger.LogTagNote($"To validate this, run with params: \n\ntagFix \"{folder.FullName}\"");
        }

        private void Log(string message, LogType logType = LogType.Process)
        {
            string header = "TagProcessor";
            ConsoleLogger.Log($"\t{header} - {message}", logType);
        }
    }
}