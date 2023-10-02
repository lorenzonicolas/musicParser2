using DTO;
using FileSystem;
using musicParser.DTO;
using musicParser.Utils.Loggers;
using System.IO.Abstractions;

namespace musicParser.TagProcess
{

    public class TagsUtils : ITagsUtils
    {
        static readonly int MIN_YEAR = 1950;
        static readonly List<char> allowedChars = new() { ':', '?', '\\', '/', '*', '?', '"', '<', '>', '|', '\'', ',' };
        public readonly List<string> AcceptedGenres = new()
        {
            "Black Metal",
            "Atmospheric Black Metal",
            "Depressive Black Metal",
            "Symphonic Black Metal",
            "Black Thrash Metal",
            "Thrash Metal",
            "Heavy Metal",
            "Industrial Metal",
            "Doom Metal",
            "Melodic Death Metal",
            "Death Doom Metal",
            "Funeral Doom Metal",
            "Death / Doom Metal",
            "Death Metal",
            "Brutal Death Metal",
            "Power Metal",
            "Folk Metal",
            "Gothic Metal",
            "Groove Metal",
            "Viking Metal",
            "Heavy Metal",
            "EBM",
            "Classic",
            "Deathcore"
        };
        private readonly IFileSystemUtils FileSystemUtils;
        private readonly IConsoleLogger ConsoleLogger;
        private readonly IFileSystem FS;

        public TagsUtils(
            IFileSystemUtils fileSystemUtils,
            IConsoleLogger consoleLogger,
            IFileSystem fs)
        {
            FileSystemUtils = fileSystemUtils;
            ConsoleLogger = consoleLogger;
            FS = fs;
        }

        public bool IsValidYear(int year)
        {
            return (year > MIN_YEAR && year <= DateTime.Now.Year);
        }

        public int? GetYear(IDirectoryInfo cdFolder, IExecutionLogger logger)
        {
            try
            {
                var anyAlbumSong = FileSystemUtils.GetAnyFolderSong(cdFolder);

                //Couldn't find any song on this folder
                if (anyAlbumSong == null)
                {
                    ConsoleLogger.Log("\tNo song found on folder", LogType.Information);
                    return null;
                }

                using var tagFile = TagLib.File.Create(anyAlbumSong.FullName);
                return Convert.ToInt32(tagFile.Tag.Year);
            }
            catch (Exception ex)
            {
                var exMessage = string.Format("\tSomething went wrong trying to retrieve year tag on {0}.\n\t{1}", cdFolder.Name, ex.Message);
                ConsoleLogger.Log(exMessage, LogType.Information);
                logger.Log(exMessage);
                return null;
            }
        }

        public bool IsInvalidGenre(string[] genres)
        {
            if(genres.Length < 1)
            {
                return true;
            }

            if (genres.Length > 1)
            {
                return true;
            }

            if (!AcceptedGenres.Contains(genres[0]))
            {
                return true;
            }

            return false;
        }

        public string GetArtistFromTag(IDirectoryInfo folder, IExecutionLogger logger)
        {
            try
            {
                var anyAlbumSong = FileSystemUtils.GetAnyFolderSong(folder);

                //Couldn't find any song on this folder
                if (anyAlbumSong == null)
                {
                    ConsoleLogger.Log("\tNo song found on folder", LogType.Information);
                    return string.Empty;
                }

                using var tagFile = TagLib.File.Create(anyAlbumSong.FullName);
                var tags = tagFile.Tag;
                return tags.FirstPerformer;
            }
            catch (Exception ex)
            {
                var exMessage = string.Format("\tSomething went wrong trying to retrieve band tag on {0}.\n\t{1}", folder.Name, ex.Message);
                ConsoleLogger.Log(exMessage, LogType.Information);
                logger.Log(exMessage);
                return string.Empty;
            }
        }

        public string? GetAlbumFromTag(IDirectoryInfo folder, IExecutionLogger logger)
        {
            try
            {
                var anyAlbumSong = FileSystemUtils.GetAnyFolderSong(folder);

                //Couldn't find any song on this folder
                if (anyAlbumSong == null)
                {
                    ConsoleLogger.Log("\tNo song found on folder", LogType.Information);
                    logger.Log("No song found on folder");
                    return null;
                }

                using var tagFile = TagLib.File.Create(anyAlbumSong.FullName);
                var tags = tagFile.Tag;
                return tags.Album;
            }
            catch (Exception ex)
            {
                var exMessage = string.Format("\tSomething went wrong trying to retrieve album tag on {0}.\n\t{1}", folder.Name, ex.Message);
                ConsoleLogger.Log(exMessage, LogType.Information);
                logger.Log(exMessage);
                return null;
            }
        }

        public string GetAlbumGenreFromTag(IDirectoryInfo albumFolder)
        {
            try
            {
                var anySong = FileSystemUtils.GetAnyFolderSong(albumFolder);
                if (anySong == null)
                {
                    throw new Exception("No song found");
                }
                else
                {
                    return GetSongGenre(anySong);
                }
            }
            catch (Exception ex)
            {
                ConsoleLogger.Log($"\tCouldn't retrieve album genre. Ex msg: {ex.Message}. Returning \'Unknown\'", LogType.Information);
                
                return "Unknown";
            }
        }

        public string GetAlbumGenre(string fullPath)
        {
            return GetAlbumGenreFromTag(FS.DirectoryInfo.New(fullPath));
        }

        public byte[]? GetCover(IFileInfo[] files)
        {
            foreach (var songFile in files)
            {
                using var tagFile = TagLib.File.Create(songFile.FullName);
                var tags = tagFile.Tag;
                var pictures = tags.Pictures;
                var pictureFound = pictures.FirstOrDefault();

                if (pictureFound != null && pictureFound.Data != null && pictureFound.Data.Count > 1)
                {
                    return pictureFound.Data.ToArray();
                }
            }

            return null;
        }

        public bool NamesAreDifferent(string str1, string str2)
        {
            return !string.Equals(str1, str2, StringComparison.InvariantCultureIgnoreCase) && !ShouldSkipNameDiscrepancy(str1, str2);
        }

        public void UpdateFolderGenre(IDirectoryInfo folder)
        {
            Console.Write("New genre: ");
            var genreFromUser = Console.ReadLine();

            while(string.IsNullOrEmpty(genreFromUser))
            {
                Console.Write("No null value please!");
                Console.Write("New genre: ");
                genreFromUser = Console.ReadLine();
            }

            Console.WriteLine("Updating \"{0}\" genre to \"{1}\"\n", folder.Name, genreFromUser);

            foreach (var songFile in FileSystemUtils.GetFolderSongs(folder))
            {
                using var tagFile = TagLib.File.Create(songFile.FullName);
                tagFile.Tag.Genres = new string[] { genreFromUser };
                tagFile.Save();
            }
        }

        public void UpdateAlbumTitleTags(IDirectoryInfo folder, string title, IExecutionLogger executionlogger)
        {
            ConsoleLogger.Log($"\tUpdating \"{folder.Name}\" album title to \"{title}\"\n");
            executionlogger.Log($"Updating {folder.Name} album title tags to {title}");
            
            foreach (var songFile in FileSystemUtils.GetFolderSongs(folder))
            {
                using var tagFile = TagLib.File.Create(songFile.FullName);
                tagFile.Tag.Album = title;
                tagFile.Save();
            }
        }

        public bool IsValidYear(string? yearFromMetalArchives)
        {
            if (string.IsNullOrEmpty(yearFromMetalArchives))
            {
                return false;
            }

            try
            {
                var year = Convert.ToInt32(yearFromMetalArchives);
                return (year > MIN_YEAR && year <= DateTime.Now.Year);
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Determines when 2 strings only have as difference a set of admitted chars
        /// </summary>
        /// <param name="str1"></param>
        /// <param name="str2"></param>
        /// <returns>True when the only differences are allowed chars</returns>
        private static bool ShouldSkipNameDiscrepancy(string str1, string str2)
        {
            List<char> diff;

            if (str1 == null || str2 == null)
                return false;

            //IEnumerable<string> set1 = title1.Split(' ').Distinct();
            //IEnumerable<string> set2 = title2.Split(' ').Distinct();

            //Convert to char array without spaces
            var set1 = str1.ToLowerInvariant().ToCharArray().ToList().Where(x => x != ' ');
            var set2 = str2.ToLowerInvariant().ToCharArray().ToList().Where(x => x != ' ');

            if (set2.Count() > set1.Count())
            {
                diff = set2.Except(set1).ToList();
            }
            else
            {
                diff = set1.Except(set2).ToList();
            }

            foreach (var charDiff in diff)
            {
                if (!allowedChars.Contains(charDiff))
                    return false;
            }

            return true;
        }

        public void UnlockFile(string fullname)
        {
            FileSystemUtils.UnlockFile(fullname);
            var unlocked = !FileSystemUtils.IsFileLocked(fullname);

            if (!unlocked) throw new Exception($"Couldn't unlock file {fullname}");
        }

        private static string GetSongGenre(IFileInfo file)
        {
            string genre;

            using (var tagFile = TagLib.File.Create(file.FullName))
            {
                genre = tagFile.Tag.FirstGenre;
            }

            return genre;
        }

        public SongInfo GetFileInformation(IFileInfo file)
        {
            var info = new SongInfo("","","");

            using (var tagFile = TagLib.File.Create(file.FullName))
            {
                info.TrackNumber = tagFile.Tag.Track.ToString("00");
                info.Title = tagFile.Tag.Title;
                info.Extension = file.Name.Split('.').Last();
            }

            return info;
        }
    }
}