using musicParser.DTO;

namespace musicParser.Utils.Loggers
{
    public interface IConsoleLogger
    {
        void Log(string message, LogType type = LogType.Process);
    }
}
