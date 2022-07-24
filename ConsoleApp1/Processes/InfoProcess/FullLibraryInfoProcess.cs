using musicParser.Utils.FileSystemUtils;

namespace musicParser.Processes.InfoProcess
{
    // TODO
    public class FullLibraryInfoProcess: IProcess
    {
        private readonly IFileSystemUtils FileSystemUtils;

        public FullLibraryInfoProcess(IFileSystemUtils fileSystemUtils)
        {
            FileSystemUtils = fileSystemUtils;
        }

        public object Execute(string folderToProcess)
        {
            Console.WriteLine("\n\n===\t\tFull library scan\t\t===");

            //FileSystemUtils.CheckMainOutputDirectory();

            Console.WriteLine("\n\nPress any key to begin...\n");
            Console.ReadLine();

            //infoActions.GatherInfo(true);

            //printInformation(fullRows);

            Console.WriteLine("\nDone! Press any key...\n");
            Console.ReadLine();

            return string.Empty;
        }
    }
}
