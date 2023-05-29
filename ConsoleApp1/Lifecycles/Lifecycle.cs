using Microsoft.Extensions.Configuration;
using musicParser.DTO;
using musicParser.Processes.FilesProcess;
using musicParser.Processes.InfoProcess;
using musicParser.TagProcess;
using musicParser.Utils.FileSystemUtils;
using musicParser.Utils.Loggers;
using System.IO.Abstractions;

namespace musicParser.Processes
{
    public class LifecycleProcess : ILifecycleProcess
    {
        private readonly string ERROR_DIR;
        private readonly string MANUAL_FIX_DIR;
        private readonly string DONE_DIR;
        private readonly string TAG_DIR;
        private readonly string WORKING_DIR;

        private readonly IParseFileProcess fileProcessor;
        private readonly IRenameFoldersProcess folderProcessor;
        private readonly INewAlbumsInfoProcess newAlbumsProcessor;
        private readonly IFileSystem fs;
        private readonly ITagProcess tagProcessor;
        private readonly IExecutionLogger loggerInstance;
        private readonly IConsoleLogger ConsoleLogger;
        private readonly IFileSystemUtils FileSystemUtils;

        public LifecycleProcess(
            IExecutionLogger logger,
            IConsoleLogger consoleLogger,
            IFileSystemUtils fsUtils,
            ITagProcess TagProcessor,
            IRenameFoldersProcess FolderProcessor,
            IParseFileProcess FileProcessor,
            INewAlbumsInfoProcess NewAlbumsProcessor,
            IFileSystem FS,
            IConfiguration config)
        {
            loggerInstance = logger;
            ConsoleLogger = consoleLogger;
            FileSystemUtils = fsUtils;
            tagProcessor = TagProcessor;
            folderProcessor = FolderProcessor;
            fileProcessor = FileProcessor;
            newAlbumsProcessor = NewAlbumsProcessor;
            fs = FS;

            ERROR_DIR = config.GetValue<string>("error_dir");
            MANUAL_FIX_DIR = config.GetValue<string>("manual_fix_dir");
            DONE_DIR = config.GetValue<string>("done_dir");
            TAG_DIR = config.GetValue<string>("tag_fix_dir");
            WORKING_DIR = config.GetValue<string>("working_dir");
        }

        public void Execute(string folderToProcess, bool generateLogOnOK)
        {
            ConsoleLogger.Log("\t\tLIFECYCLE\n\n");
            
            var inputFolders = fs.DirectoryInfo.FromDirectoryName(folderToProcess).GetDirectories();

            foreach (var folder in inputFolders)
            {
                // Ensure that every folder processes is an album folder only, not artists.
                if (FileSystemUtils.IsArtistFolder(folder))
                {
                    this.Execute(folder.FullName, generateLogOnOK);
                    continue;
                }

                var parsedFolder = folder.FullName;

                try
                {
                    loggerInstance.StartExecutionLog();
                    ConsoleLogger.Log($"\nLIFECYCLE - Processing folder: {folder.FullName}\n", LogType.Information);

                    // Rename folder processing: parse file folder name to expected format
                    parsedFolder = (string)folderProcessor.Execute(folder.FullName);

                    if (parsedFolder.Contains(MANUAL_FIX_DIR))
                    {
                        // This folder was moved to manual fix queue. Finish here the lifecycle for this folder.
                        loggerInstance.ExportLogFile(parsedFolder ?? folderToProcess, generateLogOnOK);
                        continue;
                    }

                    // File processing: search for and rename FRONT image, and parse file names to expected format
                    parsedFolder = (string)fileProcessor.Execute(parsedFolder);

                    if (parsedFolder.Contains(MANUAL_FIX_DIR))
                    {
                        // This folder was moved to manual fix queue. Finish here the lifecycle for this folder.
                        loggerInstance.ExportLogFile(parsedFolder ?? folderToProcess, generateLogOnOK);
                        continue;
                    }

                    // Fix tags if can auto-solve, otherwise generate a README with the issue.
                    var needManualFix = (bool)tagProcessor.Execute(parsedFolder);

                    // Perfect parsed album. Let's add it to backup file and metadata in case it's not present
                    if (!needManualFix)
                    {
                        newAlbumsProcessor.Execute(parsedFolder);
                        parsedFolder = FileSystemUtils.MoveProcessedFolder(parsedFolder, DONE_DIR);
                        ConsoleLogger.Log("Moved to DONE", LogType.Success);
                    }
                    else
                    {
                        parsedFolder = FileSystemUtils.MoveProcessedFolder(parsedFolder, TAG_DIR);
                        ConsoleLogger.Log("Moved to TAG_FIX", LogType.Warning);
                    }

                    loggerInstance.ExportLogFile(parsedFolder ?? folderToProcess, generateLogOnOK);

                    ConsoleLogger.Log($"\nLIFECYCLE - End Processing folder: {parsedFolder}\n", LogType.Information);
                }
                catch (Exception ex)
                {
                    loggerInstance.LogError("Folder processing terminated because of error: " + ex.Message);
                    ConsoleLogger.Log("Folder processing terminated because of error: " + ex.Message, LogType.Error);
                    loggerInstance.ExportLogFile(parsedFolder);

                    var shouldMove = new List<string> { MANUAL_FIX_DIR, TAG_DIR, WORKING_DIR }.Any(x=>parsedFolder.Contains(x));

                    if (shouldMove)
                    {
                        FileSystemUtils.MoveFolder(parsedFolder, ERROR_DIR);
                    }
                    else
                    {
                        FileSystemUtils.CopyFolder(parsedFolder, ERROR_DIR);
                    }

                    ConsoleLogger.Log("Moved to ERROR_DIR", LogType.Error);

                    ConsoleLogger.Log($"\nLIFECYCLE - End Processing folder: {parsedFolder}\n", LogType.Information);
                    continue;
                }
            }
        }
    }
}