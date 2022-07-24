using musicParser.Processes.InfoProcess;
using musicParser.Utils.Loggers;
using System.Diagnostics;

namespace musicParser.Processes
{
    public class SyncLifecycle
    {
        private readonly IExecutionLogger loggerInstance;
        private readonly IInfoActions processor;

        public SyncLifecycle(
            IExecutionLogger ExecutionLogger,
            IInfoActions info)
        {
            loggerInstance = ExecutionLogger;
            processor = info;
        }

        public void Execute(string folderToSync)
        {
            Console.Clear();

            try
            {
                loggerInstance.StartExecutionLog();

                try
                {
                    Console.WriteLine($"Sync metadata - Source: {folderToSync}\n\n");

                    var sw = new Stopwatch();
                    
                    sw.Start();
                    processor.Sync(folderToSync, updateDeletes: true);
                    sw.Stop();

                    // Format and display the TimeSpan value.
                    TimeSpan ts = sw.Elapsed;
                    string elapsedTime = string.Format("{0:00}:{1:00}.{2:00}",
                        ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
                    Console.WriteLine("RunTime " + elapsedTime);

                    //TODO
                    //loggerInstance.ExportLogFile(folderToProcess);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("-Execution terminated because of error- " + ex.Message);
                    loggerInstance.LogError("-Execution terminated because of error- " + ex.Message);
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("ERROR\n" + ex);
                Console.ResetColor();
            }

            Console.WriteLine("Press any key to finish...");
            Console.ReadKey();
        }
    }
}