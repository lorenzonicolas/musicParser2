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
            string? band = "", album = "";

            Console.Clear();
            Console.WriteLine("\tAlbum cover downloader\n");

            Console.Write("Band (or `Exit` to quit): ");
            band = Console.ReadLine();

            while (!band.Equals("exit", StringComparison.InvariantCultureIgnoreCase))
            {
                Console.Write("Album name: ");
                album = Console.ReadLine();

                Console.WriteLine($"\nTrying to get {album} folder image...");
                var bytes = await metadataService.DownloadAlbumCoverAsync(band, album);

                if (bytes != null)
                {
                    Console.WriteLine($"Image found! :)\nSaving folder image to Desktop...");
                    var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    File.WriteAllBytes(Path.Combine(desktop, $"FRONT_{band}_{album}.jpg"), bytes);
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
    }
}
