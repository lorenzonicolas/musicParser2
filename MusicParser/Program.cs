using FileSystem;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using musicParser.GoogleDrive;
using musicParser.Metadata;
using musicParser.MetalArchives;
using musicParser.Processes;
using musicParser.Processes.FilesProcess;
using musicParser.Processes.InfoProcess;
using musicParser.Spotify;
using musicParser.TagProcess;
using musicParser.Utils.Loggers;
using MusicParser.Processes.InfoProcess;
using MusicParser.Utils.HttpClient;
using Regex;
using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;

namespace musicParser
{
    [ExcludeFromCodeCoverage]
    static class Program
    {
        public static IConfigurationRoot Configuration { get; private set; }

        static async Task Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.InputEncoding = System.Text.Encoding.Unicode;

            using IHost host = CreateHostBuilder(args).Build();

            //await host.RunAsync();

            var folderToProcess = Configuration.GetValue<string>("folderToProcess");
            var folderToProcessTags = Configuration.GetValue<string>("tag_fix_dir");
            var generateLogOnOK = Configuration.GetValue<bool>("generateLogOnOK");

            var arguments = new List<string>(args);

            try
            {
                if (arguments.Contains("printTracklist"))
                {
                    ActivatorUtilities.CreateInstance<PrintTracklistProcess>(host.Services).Execute();
                }
                else if (arguments.Contains("tagFix"))
                {
                    ActivatorUtilities.CreateInstance<TagFixesLifecycle>(host.Services).Execute(folderToProcessTags, generateLogOnOK);
                }
                else if (arguments.Contains("countryFix"))
                {
                    ActivatorUtilities.CreateInstance<CountryFixesLifecycle>(host.Services).Execute().GetAwaiter().GetResult();
                }
                else if (arguments.Contains("resync"))
                {
                    ActivatorUtilities.CreateInstance<SyncLifecycle>(host.Services).Execute(folderToProcess);
                }
                else if (arguments.Contains("downloadAlbumCover"))
                {
                    await ActivatorUtilities.CreateInstance<DownloadAlbumCoverProcess>(host.Services).Execute();
                }
                else
                {
                    ActivatorUtilities.CreateInstance<LifecycleProcess>(host.Services).Execute(folderToProcess, generateLogOnOK);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unhandled exception happened: {ex}");
                throw;
            }
        }

        static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((hostingContext, configuration) =>
            {
                configuration.Sources.Clear();
                configuration
                        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

                IConfigurationRoot configurationRoot = configuration.Build();
                Configuration = configurationRoot;
            })
            .ConfigureServices((services) =>
            {
                services.AddHostedService<MusicParser.ConsoleService>();
                services.AddSingleton<IHttpClient, MusicParser.Utils.HttpClient.HttpClient>();
                services.AddSingleton<IConsoleLogger, ConsoleLogger>();
                services.AddSingleton<IExecutionLogger, ExecutionLogger>();
                services.AddSingleton<IRegexUtils, RegexUtils>();
                services.AddSingleton<ITagsUtils, TagsUtils>();
                services.AddSingleton<IFileSystem, System.IO.Abstractions.FileSystem>();
                services.AddSingleton<IFileSystemUtils, SomeUtils>();
                services.AddSingleton<IMetadataService, MetadataService>();
                services.AddSingleton<IMetalArchivesAPI, MetalArchivesAPI>();
                services.AddSingleton<IMetalArchivesService, MetalArchivesService>();
                services.AddSingleton<ISpotifyAPI, SpotifyAPIimplemen>();
                services.AddSingleton<ISpotifyService, SpotifyService>();
                services.AddSingleton<IGoogleDriveAPI, GoogleDriveAPI>();
                services.AddSingleton<IGoogleDriveService, GoogleDriveService>();
                services.AddSingleton<IInfoActions, InfoActions>();
                services.AddSingleton<IParseFileProcess, ParseFileProcess>();
                services.AddSingleton<IRenameFoldersProcess, RenameFoldersProcess>();
                services.AddSingleton<INewAlbumsInfoProcess, NewAlbumsInfoProcess>();
                services.AddSingleton<ITagProcess, TagsProcess>();
                services.AddSingleton<ITagsFixProcess, TagsFixProcess>();
                services.AddSingleton<ILifecycleProcess, LifecycleProcess>();
                services.AddSingleton<ICountryFixesProcess, CountryFixesProcess>();
                services.AddSingleton<IPrintTracklist, PrintTracklistProcess>();
            });
    }
}
