using System.IO.Abstractions;

namespace musicParser.Utils.Loggers
{
    public class ExecutionLogger : IExecutionLogger
    {
        public string log;
        public bool hasError;
        public bool hasTagChangesNeeded;

        private readonly IFileSystem fs;

        public ExecutionLogger(IFileSystem fileSystem)
        {
            log = string.Empty;
            hasError = false;
            hasTagChangesNeeded = false;
            fs = fileSystem;
        }

        public void StartExecutionLog()
        {
            log = string.Empty;
            hasError = false;
            hasTagChangesNeeded = false;
        }

        public void Log(string text)
        {
            log += text + "\n";
        }

        public void ExportLogFile(string destiny, bool generateLogOnOK = true)
        {
            if (!generateLogOnOK && !hasError && !hasTagChangesNeeded)
            {
                return;
            }

            var fileName = DateTime.Now.ToString("yyyy-MM-dd--HH-mm-ss") + "__";

            fileName += hasError ? "ERROR_executionLog" : 
                            hasTagChangesNeeded ? "TAG_CHECK_executionLog" : "OK_executionLog";

            fileName += ".txt";

            if (fs.Directory.Exists(destiny))
            {
                fs.File.AppendAllText(fs.Path.Combine(destiny, fileName), log, System.Text.Encoding.UTF8);
            }
            else
            {
                throw new Exception("Invalid directory");
            }
        }

        public void LogError(Exception ex)
        {
            Log("\tERROR!\t");
            Log(ex.Message);
            hasError = true;
        }

        public void LogError(string error)
        {
            Log("\tERROR!\t");
            Log(error);
            hasError = true;
        }

        public void LogTagNote(string msg)
        {
            Log(msg);
            hasTagChangesNeeded = true;
        }
    }
}