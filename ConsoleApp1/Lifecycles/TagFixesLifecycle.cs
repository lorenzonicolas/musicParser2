using Microsoft.Extensions.Configuration;
using musicParser.Processes.InfoProcess;
using musicParser.TagProcess;
using musicParser.Utils.FileSystemUtils;
using musicParser.Utils.Loggers;

namespace musicParser.Processes
{
    public class TagFixesLifecycle
    {
        private readonly INewAlbumsInfoProcess newAlbumsProcessor;
        private readonly ITagsFixProcess tagFixProcessor;
        private readonly IFileSystemUtils FileSystemUtils;
        private readonly IExecutionLogger loggerInstance;

        private readonly string DONE_DIR;

        public TagFixesLifecycle(
            IExecutionLogger executionLogger,
            INewAlbumsInfoProcess NewAlbumsProcessor,
            ITagsFixProcess TagFixProcessor,
            IFileSystemUtils fileSystemUtils,
            IConfiguration config)
        {
            loggerInstance = executionLogger;
            newAlbumsProcessor = NewAlbumsProcessor;
            tagFixProcessor = TagFixProcessor;
            FileSystemUtils = fileSystemUtils;
            DONE_DIR = config.GetValue<string>("done_dir");
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