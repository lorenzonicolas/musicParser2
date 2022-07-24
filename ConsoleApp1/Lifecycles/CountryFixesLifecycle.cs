using musicParser.Utils.Loggers;

namespace musicParser.Processes
{
    public class CountryFixesLifecycle
    {
        private readonly IProcess countryFixProcessor;
        
        private readonly IExecutionLogger loggerInstance;

        public CountryFixesLifecycle(
            IExecutionLogger logger,
            IProcess countryFix)
        {
            loggerInstance = logger;
            countryFixProcessor = countryFix;
        }

        public void Execute()
        {
            Console.Clear();

            try
            {
                loggerInstance.StartExecutionLog();

                try
                {
                    Console.WriteLine("Country metadata fix\n\n");
                    countryFixProcessor.Execute(string.Empty);

                    //TODO
                    //loggerInstance.ExportLogFile(folderToProcess);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("-Execution terminated because of error- " + ex.Message);
                    loggerInstance.LogError("-Execution terminated because of error- " + ex.Message);
                    //todo
                    //loggerInstance.ExportLogFile(folder.FullName);
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