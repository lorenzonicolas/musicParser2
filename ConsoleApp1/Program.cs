using Microsoft.Extensions.DependencyInjection;
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
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO.Abstractions;

namespace musicParser
{
    class Program
    {
        static void Main(string[] args)
        {
            var folderToProcess = ConfigurationManager.AppSettings["folderToProcess"].ToString();
            var folderToProcessTags = ConfigurationManager.AppSettings["tag_fix_dir"].ToString();
            var generateLogOnOK = bool.Parse(ConfigurationManager.AppSettings["generateLogOnOK"]);

            var arguments = new List<string>(args);

            //setup our DI
            ServiceProvider serviceProvider = BuildContainer();

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
                    serviceProvider.GetService<ILifecycleProcess>().Execute(folderToProcess, generateLogOnOK);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unhandled exception happened: {ex}");
                Console.ReadLine();
                throw;
            }

            Console.WriteLine("Press any key to finish...");
            Console.ReadLine();
        }

        private static ServiceProvider BuildContainer()
        {
            var serviceProvider = new ServiceCollection()
                            //.AddLogging()
                            .AddSingleton<IConsoleLogger, ConsoleLogger>()
                            .AddSingleton<IExecutionLogger, ExecutionLogger>()
                            .AddSingleton<IRegexUtils, RegexUtils>()
                            .AddSingleton<ITagsUtils, TagsUtils>()
                            .AddSingleton<IFileSystem, FileSystem>()
                            .AddSingleton<IFileSystemUtils, FileSystemUtils>()
                            .AddSingleton<IMetadataService, MetadataService>()
                            .AddSingleton<IMetalArchivesAPI, MetalArchivesAPI>()
                            .AddSingleton<IMetalArchivesService, MetalArchivesService>()
                            .AddSingleton<ISpotifyAPI, SpotifyAPIimplemen>()
                            .AddSingleton<ISpotifyService, SpotifyService>()
                            .AddSingleton<IGoogleDriveAPI, GoogleDriveAPI>()
                            .AddSingleton<IGoogleDriveService, GoogleDriveService>()
                            .AddSingleton<IInfoActions, InfoActions>()
                            .AddSingleton<IParseFileProcess, ParseFileProcess>()
                            .AddSingleton<IRenameFoldersProcess, RenameFoldersProcess>()
                            .AddSingleton<INewAlbumsInfoProcess, NewAlbumsInfoProcess>()
                            .AddSingleton<ITagProcess, TagsProcess>()
                            .AddSingleton<ILifecycleProcess, LifecycleProcess>()
                            .BuildServiceProvider();
            return serviceProvider;
        }
    }
}
