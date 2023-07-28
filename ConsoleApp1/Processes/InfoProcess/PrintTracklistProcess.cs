using musicParser.Metadata;
using musicParser.Utils.FileSystemUtils;
using musicParser.Utils.Regex;
using System.Diagnostics;
using System.IO.Abstractions;

namespace MusicParser.Processes.InfoProcess
{
    interface IPrintTracklist
    {
        void Execute();
    }

    public class PrintTracklistProcess : IPrintTracklist
    {
        private readonly IFileSystemUtils utils;
        private readonly IFileSystem FS;
        private readonly IRegexUtils regex;
        private readonly IMetadataService metadata;

        private readonly string template =
            $"BAND_NAME - BAND_ALBUM (BAND_YEAR) [FullAlbum]\n" +
            $"BAND_NAME - BAND_ALBUM\n\n" +
            $"----- TRACKLIST ------\n" +
            $"BAND_TRACKLIST\n\n" +
            $"Due to Copyright, this channel is not monetized.\n" +
            $"If you want to support this channel, you can buy me a beer here:\n" +
            $"https://www.buymeacoffee.com/ExtremeAlbums\n\n\n" +
            $"----- RELEASE INFO -----\n\n" +
            $"Band: BAND_NAME\n" +
            $"Album: BAND_ALBUM\n" +
            $"Year: BAND_YEAR\n" +
            $"Genre: BAND_GENRE\n" +
            $"Country: BAND_COUNTRY\n" +
            $"LINK_TO_METALARCHIVES\n" +
            $"https://duckduckgo.com/?q=%5Csite%3Ametal-archives.com+BAND_NAME";

        public PrintTracklistProcess(
            IFileSystemUtils fileSystemUtils,
            IFileSystem fs,
            IRegexUtils regexUtils,
            IMetadataService metadataService)
        {
            utils = fileSystemUtils;
            FS = fs;
            regex = regexUtils;
            metadata = metadataService;
        }

        public void Execute()
        {
            string? fullPath = "";

            while(fullPath != "exit")
            {
                Console.WriteLine("Enter the album full path (or exit):");
                fullPath = Console.ReadLine();

                if(string.Equals("exit", fullPath, StringComparison.InvariantCultureIgnoreCase))
                {
                    return;
                }

                if (string.IsNullOrEmpty(fullPath) || !utils.ValidateDirectory(fullPath))
                {
                    Console.WriteLine($"Invalid path: {fullPath}");
                    return;
                }

                var directory = FS.DirectoryInfo.FromDirectoryName(fullPath);

                if (!utils.IsAlbumFolder(directory))
                {
                    Console.WriteLine($"This is not recognized as an album folder: {fullPath}");
                    return;
                }

                var songFiles = utils
                    .GetFolderSongs(directory)
                    .OrderBy(x => x.Name);

                var count = TimeSpan.Zero;
                var trackList = string.Empty;
                var output = template;

                var bandName = directory.Parent.Name;
                var bandGenre = metadata.GetBandGenre(bandName);
                var albumInfo = regex.GetFolderInformation(directory.Name);
                var albumName = albumInfo.Album;

                foreach (var song in songFiles)
                {
                    var fileInfo = regex.GetFileInformation(song.Name);

                    using var tags = TagLib.File.Create(song.FullName);
                    trackList += $"{count.Minutes:00}:{count.Seconds:00} - {fileInfo.TrackNumber}. {fileInfo.Title}\n";
                    count += tags.Properties.Duration;
                }

                output = template
                    .Replace("BAND_NAME", bandName)
                    .Replace("BAND_ALBUM", albumName)
                    .Replace("BAND_TRACKLIST", trackList)
                    .Replace("BAND_YEAR", albumInfo.Year)
                    .Replace("BAND_GENRE", metadata.GetBandGenre(bandName))
                    .Replace("BAND_COUNTRY", metadata.GetBandCountry(bandName))
                    .Replace("URL_NAME", bandName.Replace(' ', '_'));

                Console.WriteLine("Output:\n\n" + trackList);

                if (tryCopyToClipboard(output))
                {
                    Console.WriteLine("Copied to clipboard! :)");
                }
            }
        }

        private bool tryCopyToClipboard(string value)
        {
            try
            {
                if (value == null)
                {
                    throw new ArgumentNullException("Attempt to set clipboard with null");
                }

                Process clipboardExecutable = new()
                {
                    StartInfo = new ProcessStartInfo // Creates the process
                    {
                        RedirectStandardInput = true,
                        FileName = @"clip",
                    }
                };
                clipboardExecutable.Start();

                clipboardExecutable.StandardInput.Write(value); // CLIP uses STDIN as input.
                                                                // When we are done writing all the string, close it so clip doesn't wait and get stuck
                clipboardExecutable.StandardInput.Close();

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
