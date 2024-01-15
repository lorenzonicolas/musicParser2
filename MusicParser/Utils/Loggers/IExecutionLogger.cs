using System;

namespace musicParser.Utils.Loggers
{
    public interface IExecutionLogger
    {
        void StartExecutionLog();
        void Log(string text);
        void ExportLogFile(string destiny, bool generateLogOnOK = true);
        void LogError(Exception ex);
        void LogError(string error);
        void LogTagNote(string msg);
    }
}
