using musicParser.Processes.InfoProcess;
using musicParser.Utils.Loggers;

namespace musicParser.Processes
{
    public class CountryFixesLifecycle
    {
        private readonly ICountryFixesProcess countryFixProcessor;        
        private readonly IConsoleLogger loggerInstance;

        public CountryFixesLifecycle(
            IConsoleLogger logger,
            ICountryFixesProcess countryFix)
        {
            loggerInstance = logger;
            countryFixProcessor = countryFix;
        }

        public async Task Execute()
        {
            Console.Clear();

            try
            {
                loggerInstance.Log("Country metadata fix\n\n");
                await countryFixProcessor.Execute();
            }
            catch (Exception ex)
            {
                loggerInstance.Log("-Execution terminated because of error- " + ex.Message, DTO.LogType.Error);
            }
            
        }
    }
}