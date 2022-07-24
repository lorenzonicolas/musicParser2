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
using musicParser.Utils.FileSystemUtils;
using musicParser.Utils.Loggers;
using musicParser.Utils.Regex;
using System.IO.Abstractions;

namespace musicParser
{
    static class Program
    {
        public static IConfigurationRoot Configuration { get; private set; }

        static void Main(string[] args)
        {
            using IHost host = CreateHostBuilder(args).Build();

            //await host.RunAsync();

            var folderToProcess = Configuration.GetValue<string>("folderToProcess");
            var folderToProcessTags = Configuration.GetValue<string>("tag_fix_dir");
            var generateLogOnOK = Configuration.GetValue<bool>("generateLogOnOK");

            var arguments = new List<string>(args);

            try
            {
                if (arguments.Contains("tagFix"))
                {
                    //new TagFixesLifecycle().Execute(folderToProcessTags, generateLogOnOK);
                }
                else if (arguments.Contains("countryFix"))
                {
                    //new CountryFixesLifecycle().Execute();
                }
                else if (arguments.Contains("resync"))
                {
                    //new SyncLifecycle().Execute(folderToProcess);
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
                services.AddSingleton<IConsoleLogger, ConsoleLogger>();
                services.AddSingleton<IExecutionLogger, ExecutionLogger>();
                services.AddSingleton<IRegexUtils, RegexUtils>();
                services.AddSingleton<ITagsUtils, TagsUtils>();
                services.AddSingleton<IFileSystem, FileSystem>();
                services.AddSingleton<IFileSystemUtils, FileSystemUtils>();
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
                services.AddSingleton<ILifecycleProcess, LifecycleProcess>();
            });
    }
}
