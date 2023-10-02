using musicParser.MetalArchives;

namespace MusicParser.Processes.InfoProcess
{
    interface IDownloadAlbumCoverProcess
    {
        Task Execute();
    }

    public class DownloadAlbumCoverProcess : IDownloadAlbumCoverProcess
    {
        private readonly IMetalArchivesService metadataService;

        public DownloadAlbumCoverProcess(IMetalArchivesService MetadataService)
        {
            metadataService = MetadataService;
        }

        public async Task Execute()
        {
            Console.Clear();
            Console.WriteLine("\tAlbum cover downloader\n");

            Console.Write("Band (or `Exit` to quit): ");
            string? band = Console.ReadLine();
            byte[]? downloadedFile = null;

            while (band != null && !band.Equals("exit", StringComparison.InvariantCultureIgnoreCase))
            {
                Console.Write("Album name: ");
                string? album = Console.ReadLine();
                Console.WriteLine($"\nTrying to get {album} folder image...");

                if(!string.IsNullOrEmpty(album))
                {
                    downloadedFile = await metadataService.DownloadAlbumCoverAsync(band, album);
                }

                if (downloadedFile != null)
                {
                    Console.WriteLine($"Image found! :)\nSaving folder image to Desktop...");
                    var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

                    var fileName = MakeValidFileName($"FRONT_{band}_{album}.jpg");
                    var destiny = Path.Combine(desktop, fileName);
                    File.WriteAllBytes(destiny, downloadedFile);
                    Console.WriteLine($"Album {album} cover successfully saved.\n");
                }
                else
                {
                    Console.WriteLine($"Couldn't find the folder image :(");
                }

                Console.Write("Band (or `Exit` to quit): ");
                band = Console.ReadLine();
            }
        }

        private static string MakeValidFileName(string name)
        {
            string invalidChars = System.Text.RegularExpressions.Regex.Escape(new string(Path.GetInvalidFileNameChars()));
            string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);

            return System.Text.RegularExpressions.Regex.Replace(name, invalidRegStr, "_");
        }
    }
}
