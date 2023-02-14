using musicParser.DTO;
using musicParser.Metadata;
using musicParser.MetalArchives;
using musicParser.Spotify;
using musicParser.Utils.FileSystemUtils;
using musicParser.Utils.Loggers;
using musicParser.Utils.Regex;
using System.IO.Abstractions;

namespace musicParser.TagProcess
{
    public interface ITagsFixProcess
    {
        object Execute(string folderToProcess);
    }

    partial class TagsFixProcess : ITagsFixProcess
    {
        private readonly IExecutionLogger _logger;
        private readonly IMetadataService metadataServices;
        private readonly IMetalArchivesService metalArchivesService;
        private readonly ISpotifyService spotifyService;
        private readonly IFileSystemUtils FileSystemUtils;
        private readonly IRegexUtils RegexUtils;
        private readonly ITagsUtils TagsUtils;
        private readonly IFileSystem FS;

        public TagsFixProcess(
            IMetadataService metadata,
            IMetalArchivesService metalArchives,
            IExecutionLogger logger,
            ISpotifyService spotify,
            IFileSystemUtils fileSystemUtils,
            IRegexUtils regexUtils,
            ITagsUtils tagsUtils,
            IFileSystem fs)
        {
            _logger = logger;
            metadataServices = metadata;
            metalArchivesService = metalArchives;
            spotifyService = spotify;
            FileSystemUtils = fileSystemUtils;
            RegexUtils = regexUtils;
            TagsUtils = tagsUtils;
            FS = fs;
        }

        public object Execute(string folderToProcess)
        {
            var folder = FS.DirectoryInfo.FromDirectoryName(folderToProcess);

            if (FileSystemUtils.IsAlbumFolder(folder))
            {
                ProcessAsAlbumFolder(folder);
            }
            else if (FileSystemUtils.IsArtistFolder(folder))
            {
                ProcessAsArtistFolder(folder);
            }

            return string.Empty;
        }

        private void ProcessAsArtistFolder(IDirectoryInfo artistFolder)
        {
            foreach (var album in FileSystemUtils.GetFolderAlbums(artistFolder))
            {
                ProcessAsAlbumFolder(album, artistFolder.Name);
            }
        }

        private void ProcessAsAlbumFolder(IDirectoryInfo album, string? overrideBandName = null)
        {
            try
            {
                var albumInfo = RegexUtils.GetFolderInformation(album.Name);

                if (!string.IsNullOrEmpty(overrideBandName))
                {
                    albumInfo.Band = album.Parent.Name;
                }

                if (string.IsNullOrEmpty(albumInfo.Band))
                {
                    throw new Exception($"Error Regex - Couldn't retrieve this folder information: {album.FullName}");
                }

                if (FileSystemUtils.AlbumContainsCDFolders(album))
                {
                    _logger.Log("Album with inner CD folders!");
                    foreach (var cd in FileSystemUtils.GetFolderAlbums(album))
                    {
                        ProcessAlbumFolder(cd, albumInfo);
                    }
                }
                else
                {
                    ProcessAlbumFolder(album, albumInfo);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Tags fix Process Error. Folder: {album.FullName} \nErr msg: {ex.Message}");
                Console.WriteLine($"Tags fix Process Error. Folder: {album.FullName} \nErr msg: {ex.Message}");
                throw;
            }
        }

        private void ProcessAlbumFolder(IDirectoryInfo folder, FolderInfo albumInfo)
        {
            var albumGenreFromMeta = metadataServices.GetBandGenre(albumInfo.Band);
            var noGenreOnMetadata = string.IsNullOrEmpty(albumGenreFromMeta) || string.Equals(albumGenreFromMeta, "Unknown", StringComparison.InvariantCultureIgnoreCase);

            if (!noGenreOnMetadata)
            {
                Console.WriteLine("Found genre on metadata... no need to fix this album.");
                _logger.Log("Found genre on metadata... no need to fix this album.");
                return;
            }

            var genresFound = GetAllGenresInFiles(folder, noGenreOnMetadata);
            
            var genreDecided = DecideGenre(folder, albumInfo, genresFound);
            OverrideGenreTag(folder, genreDecided);
        }

        private void OverrideGenreTag(IDirectoryInfo folder, string? genreDecided)
        {
            foreach (var songFile in FileSystemUtils.GetFolderSongs(folder))
            {
                try
                {
                    //In case it's readonly
                    TagsUtils.UnlockFile(songFile.FullName);
                    using var tagFile = TagLib.File.Create(songFile.FullName);
                    tagFile.Tag.Genres = new string[] { genreDecided };
                    tagFile.Save();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error on tag processing file: {songFile.FullName} - Err msg: {ex.Message}");
                    _logger.LogError($"Error on tag processing file: {songFile.FullName} - Err msg: {ex.Message}");
                    throw;
                }
            }

            _logger.Log($"Successfully set genre \"{genreDecided}\" to this album!");
            Console.WriteLine($"Successfully set genre \"{genreDecided}\" to this album!");
        }

        private List<string> GetAllGenresInFiles(IDirectoryInfo folder, bool noGenreOnMetadata)
        {
            var genresFound = new List<string>();

            foreach (var songFile in FileSystemUtils.GetFolderSongs(folder))
            {
                try
                {
                    //In case it's readonly
                    TagsUtils.UnlockFile(songFile.FullName);

                    using var tagFile = TagLib.File.Create(songFile.FullName);
                    if (noGenreOnMetadata)
                    {
                        // No metadata genre for this band. Lets add all the genres found on the songs to later evaluate them all.
                        genresFound.AddRange(tagFile.Tag.Genres);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error on tag processing file: {songFile.FullName} - Err msg: {ex.Message}");
                    _logger.LogError($"Error on tag processing file: {songFile.FullName} - Err msg: {ex.Message}");
                    throw;
                }
            }

            return genresFound.Distinct().ToList();
        }

        private string? DecideGenre(IDirectoryInfo folder, FolderInfo albumInfo, IList<string> genresFound)
        {
            Console.WriteLine($"Let's fix {albumInfo.Band} - {albumInfo.Album}");

            //Try to get genre from MetalArchives
            var genreFromMetalArchives = metalArchivesService.GetBandGenre(new AlbumInfoOnDisk()
            {
                AlbumName = albumInfo.Album,
                Band = albumInfo.Band
            }).Result;

            var genreFromSpotify = spotifyService.GetArtistGenreUsingAlbum(albumInfo.Band, albumInfo.Album);

            var canAutosolve = genresFound.Count == 1 && genreFromMetalArchives.Equals(genresFound.First());

            if (canAutosolve)
            {
                Console.WriteLine($"Same genre in tags ({genresFound.First()}) " +
                    $"than MetalArchives ({genreFromMetalArchives})." +
                    $"Autosolved it!");

                return genreFromMetalArchives;
            }

            PrintDecideGenreMessage(folder, genresFound, genreFromMetalArchives, genreFromSpotify);

            var genreConfirmed = string.Empty;
            var confirmed = false;
            while (!confirmed)
            {
                Console.WriteLine("\nPlease confirm what genre this album is (or 'M' to use the MetalArchive genre): ");
                genreConfirmed = Console.ReadLine();

                if (genreConfirmed == "M")
                {
                    genreConfirmed = genreFromMetalArchives;
                    confirmed = true;
                }
                else
                {
                    Console.WriteLine("\nAre you sure? (Y/N)\t" + genreConfirmed);
                    confirmed = Console.ReadKey(true).Key == ConsoleKey.Y;
                }
            }

            return genreConfirmed;
        }

        private static void PrintDecideGenreMessage(IDirectoryInfo folder, IList<string> genresFound, string genreFromMetalArchives, IList<string>? genreFromSpotify)
        {
            var manyGenresFound = genresFound.Count > 1;
            var noGenreFound = genresFound.Count < 1 || (genresFound.Count == 1 && (string.IsNullOrEmpty(genresFound[0]) || genresFound[0] == " "));

            if (manyGenresFound)
            {
                Console.WriteLine($"Found multiple genres on tags and has no Metadata in Drive.\nTags: {genresFound}");
            }
            else if (noGenreFound)
            {
                Console.WriteLine($"Found no genre on tags and has no Metadata in Drive.\tAlbum: {folder.Name}");
            }
            else
            {
                Console.WriteLine($"Found one genre but no Metadata in Drive\nGenre on tags: {genresFound[0]}");
            }

            if (!string.IsNullOrEmpty(genreFromMetalArchives))
            {
                Console.WriteLine($"Genres recommendation by MetalArchives: {genreFromMetalArchives}");
            }
            else
            {
                Console.WriteLine("No genres found by MetalArchives");
            }

            if (genreFromSpotify?.Count > 0)
            {
                Console.WriteLine($"Genres recommendation by Spotify: {string.Join(", ", genreFromSpotify)}");
            }
            else
            {
                Console.WriteLine("No genres found by Spotify");
            }
        }
    }
}