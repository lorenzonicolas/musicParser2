using musicParser.DTO;
using musicParser.GoogleDrive;
using musicParser.MetalArchives;
using musicParser.TagProcess;
using musicParser.Utils.Loggers;
using System.Collections.Concurrent;

namespace musicParser.Metadata
{
    public class MetadataService : IMetadataService
    {
        private readonly IGoogleDriveService GoogleService;
        private readonly IMetalArchivesService MetalArchivesService;
        private readonly IConsoleLogger ConsoleLogger;
        private readonly ITagsUtils TagsUtils;
        private readonly List<MetadataDto> metadata;

        public MetadataService(
            IGoogleDriveService driveService,
            IMetalArchivesService metalArchivesService,
            IConsoleLogger consoleLogger,
            ITagsUtils tagsUtils)
        {
            GoogleService = driveService;
            MetalArchivesService = metalArchivesService;
            ConsoleLogger = consoleLogger;
            metadata = GoogleService.GetMetadataFile();
            TagsUtils = tagsUtils;
        }

        public bool SyncMetadataFile(List<AlbumInfoOnDisk> allAlbums)
        {
            var updateNeeded = false;
            var addedEntries = new ConcurrentBag<MetadataDto>();

            Parallel.ForEach(allAlbums, (album) => 
            {
                if (!metadata.Any(x => x.Band.Equals(album.Band, StringComparison.InvariantCultureIgnoreCase))
                && !addedEntries.Any(x => x.Band.Equals(album.Band, StringComparison.InvariantCultureIgnoreCase)))
                {
                    addedEntries.Add(GenerateNewBand(album));
                    Console.WriteLine("New band metadata entry: " + album.Band);
                }
            });

            if (addedEntries.Count > 0)
            {
                var distinctItems = addedEntries.GroupBy(x => x.Band).Select(y => y.First());
                metadata.AddRange(distinctItems);
                metadata.OrderBy(x => x.Band);
                updateNeeded = true;
            }

            return updateNeeded;
        }

        public string GetBandGenre(string band)
        {
            try
            {
                var bandInMetadata = metadata.SingleOrDefault(x => x.Band == band);

                return bandInMetadata == null ? "Unknown" : bandInMetadata.Genre;
            }
            catch (InvalidOperationException)
            {
                throw new Exception(string.Format("Error! Band has multiple entries on metadata file. \"{0}\"", band));
            }
            catch (Exception ex)
            {
                throw new Exception("Error trying to get band genre from metadata: " + ex.Message);
            }
        }

        public string GetBandCountry(string band)
        {
            try
            {
                var bandInMetadata = metadata.SingleOrDefault(x => x.Band == band);

                return bandInMetadata == null ? "Unknown" : bandInMetadata.Country;
            }
            catch (InvalidOperationException)
            {
                throw new Exception(string.Format("Error! Band has multiple entries on metadata file. \"{0}\"", band));
            }
            catch (Exception ex)
            {
                throw new Exception("Error trying to get band country from metadata: " + ex.Message);
            }
        }

        private MetadataDto GenerateNewBand(AlbumInfoOnDisk albumInfo)
        {
            // Este deberia ser mas inteligente. Si no saca el pais, ver de sacarlo de Spotify
            var country = MetalArchivesService.GetBandCountry(albumInfo.Band, albumInfo.AlbumName).Result;
             
            if(string.IsNullOrEmpty(country) || string.Equals(country, "Unknown", StringComparison.InvariantCultureIgnoreCase))
            {
                // try with Spotify? Por ahora tengo como ejemplo Wednesday 13 como banda que no me trae pais porque no existe.
                // Seguro todas las de EBM pase lo mismo.
            }

            // Ver que puedo hacer con el genero en este caso...

            return new MetadataDto()
            {
                Band = albumInfo.Band,
                Country = country,
                Genre = TagsUtils.GetAlbumGenre(albumInfo.FolderPath)
            };
        }

        public bool UpdateMetadataFile()
        {
            var nullCountries = metadata
                .Where(x => x.Country == null || x.Country.Equals("Unknown", StringComparison.InvariantCultureIgnoreCase))
                .Select(x=>x.Band);

            var nullGenres = metadata
                .Where(x => x.Genre == null || x.Genre.Equals("Unknown", StringComparison.InvariantCultureIgnoreCase))
                .Select(x => x.Band);

            if (nullCountries.Count() > 0)
            {
                ConsoleLogger.Log(string.Format("Unknown band countries: {0}", string.Join(", ", nullCountries)), LogType.Error);
            }

            if (nullGenres.Count() > 0)
            {
                ConsoleLogger.Log(string.Format("Unknown band genres: {0}", string.Join(", ", nullGenres)), LogType.Error);
            }

            return GoogleService.UpdateMetadataFile(metadata.OrderBy(x=>x.Band));
        }

        public List<MetadataDto> GetCountryMetadataToFix()
        {
            return metadata.Where(
                x => string.IsNullOrEmpty(x.Country) || 
                x.Country.Equals("Unknown", StringComparison.InvariantCultureIgnoreCase) ||
                x.Country == " "
            ).ToList();
        }
    }
}