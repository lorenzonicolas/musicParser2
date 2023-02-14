using musicParser.DTO;

namespace musicParser.Utils.Loggers
{
    public class ConsoleLogger : IConsoleLogger
    {
        public void Log(string message, LogType type = LogType.Process)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            switch (type)
            {
                case LogType.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case LogType.Information:
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    break;
                case LogType.Success:
                    Console.ForegroundColor = ConsoleColor.Green;
                    break;
                case LogType.Process:
                    break;

                default:
                    throw new Exception("Invalid LogType");
            }

            Console.WriteLine(message);
            Console.ResetColor();

            //if (type == LogType.Error)
            //{
            //    File.AppendAllText(Path.Combine(Directory.GetCurrentDirectory(), "errorLog.txt"),
            //        string.Format("[{0}] - {1}\n", DateTime.Now, message),
            //        System.Text.Encoding.UTF8);
            //}
        }
    }
}