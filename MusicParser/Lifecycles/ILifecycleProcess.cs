namespace musicParser.Processes
{
    public interface ILifecycleProcess
    {
        void Execute(string folderToProcess, bool generateLogOnOK);
    }
}