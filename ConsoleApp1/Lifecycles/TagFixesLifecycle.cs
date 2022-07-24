using musicParser.Utils.FileSystemUtils;
using musicParser.Utils.Loggers;
using System;
using System.Configuration;
using System.IO;
using System.Linq;

namespace musicParser.Processes
{
    public class TagFixesLifecycle
    {
        private readonly IProcess newAlbumsProcessor;
        private readonly IProcess tagFixProcessor;
        private readonly IFileSystemUtils FileSystemUtils;
        private readonly IExecutionLogger loggerInstance;

        private readonly string DONE_DIR = ConfigurationManager.AppSettings["done_dir"].ToString();

        public TagFixesLifecycle(
            IExecutionLogger executionLogger,
            IProcess NewAlbumsProcessor,
            IProcess TagFixProcessor,
            IFileSystemUtils fileSystemUtils)
        {
            loggerInstance = executionLogger;
            newAlbumsProcessor = NewAlbumsProcessor;
            tagFixProcessor = TagFixProcessor;
            FileSystemUtils = fileSystemUtils;
        }

        public void Execute(string folderToProcess, bool generateLogFile)
        {
            Console.Clear();

            try
            {
                loggerInstance.StartExecutionLog();

                if (string.IsNullOrEmpty(folderToProcess) || !Directory.Exists(folderToProcess))
                {
                    Console.WriteLine("ERROR - invalid folder path to process - Invalid");
                    loggerInstance.LogError("ERROR - invalid folder path to process - Invalid");
                    return;
                }

                var folders = FileSystemUtils.GetFolderArtists(folderToProcess)
                                .Concat(FileSystemUtils.GetFolderAlbums(folderToProcess));

                foreach (var folder in folders)
                {
                    var currentPath = folder.FullName;

                    try
                    {
                        tagFixProcessor.Execute(folder.FullName);

                        newAlbumsProcessor.Execute(folder.FullName);

                        loggerInstance.ExportLogFile(folder.FullName, generateLogFile);

                        currentPath = FileSystemUtils.MoveProcessedFolder(folder.FullName, DONE_DIR);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Folder execution terminated because of error- " + ex.Message);
                        loggerInstance.LogError("Folder execution terminated because of error- " + ex.Message);
                        loggerInstance.ExportLogFile(currentPath);
                        continue;
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR\n" + ex);
            }

            Console.WriteLine("Press any key to finish...");
            Console.ReadKey();
        }
    }
}