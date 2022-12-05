using Dasync.Collections;
using Google.Apis.Logging;
using musicParser.GoogleDrive;
using musicParser.Metadata;
using musicParser.MetalArchives;
using musicParser.Utils.Loggers;

namespace musicParser.Processes.InfoProcess
{
    public interface ICountryFixesProcess
    {
        Task<object> Execute();
    }

    public class CountryFixesProcess : ICountryFixesProcess
    {
        private readonly IMetadataService _metadataService;
        private readonly IMetalArchivesService _metalArchivesService;
        private readonly IConsoleLogger _consoleLogger;
        private readonly List<DTO.AlbumInfoBackupDto> backupFile;

        public CountryFixesProcess(
            IGoogleDriveService googleDrive,
            IMetadataService metadataService,
            IMetalArchivesService metalArchivesService,
            IConsoleLogger consoleLogger)
        {
            _metadataService = metadataService;
            _metalArchivesService = metalArchivesService;
            _consoleLogger = consoleLogger;

            backupFile = googleDrive.GetBackupFile();
        }

        public async Task<object> Execute()
        {
            var brokenCountriesList = _metadataService.GetCountryMetadataToFix();
            if (brokenCountriesList.Count < 1)
            {
                _consoleLogger.Log("No broken countries in metadata.", DTO.LogType.Success);
            }

            _consoleLogger.Log("List of bands with broken country:\n");
            foreach (var item in brokenCountriesList)
            {
                _consoleLogger.Log($"\t- {item.Band}. Country: {item.Country}");
            }

            await brokenCountriesList.ParallelForEachAsync(async bandDto =>
            {
                var firstBandAlbumFound = backupFile.FirstOrDefault(b => b.Band.Equals(bandDto.Band))?.AlbumName;
                string countryFromMetalArchives = await _metalArchivesService.GetBandCountry(bandDto.Band, firstBandAlbumFound);

                if (!string.IsNullOrEmpty(countryFromMetalArchives) && !countryFromMetalArchives.Equals("Unknown"))
                {
                    _consoleLogger.Log($"Updating {bandDto.Band} country to: {countryFromMetalArchives}", DTO.LogType.Success);
                    bandDto.Country = countryFromMetalArchives;
                }
                else
                {
                    _consoleLogger.Log($"Band '{bandDto.Band}' country couldn't be retrieved");
                    // FixByConsoleEntry(bandDto);
                }
            });

            _consoleLogger.Log("\nUploading backup file to Google Drive...\n");

            var successUpload = _metadataService.UpdateMetadataFile();

            if (!successUpload)
                throw new Exception("Something went wrong... I didn't uploaded the file to Google Drive!");

            return Task.FromResult(successUpload);
        }

        private static void FixByConsoleEntry(DTO.MetadataDto bandDto)
        {
            var pressedKey = ConsoleKey.C;
            while (pressedKey == ConsoleKey.C)
            {
                Console.Write($"{bandDto.Band}: ");
                var newCountryName = Console.ReadLine();
                Console.WriteLine("\tConfirm (Y), Cancel (C) or Skip (any)?");
                pressedKey = Console.ReadKey(true).Key;

                switch (pressedKey)
                {
                    case ConsoleKey.Y:
#pragma warning disable CS8601 // Possible null reference assignment.
                        bandDto.Country = newCountryName;
#pragma warning restore CS8601 // Possible null reference assignment.
                        break;
                    case ConsoleKey.C:
                        break;
                    default:
                        Console.WriteLine("Skipped");
                        continue;
                }
            }
        }
    }
}
