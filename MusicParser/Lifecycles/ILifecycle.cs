namespace musicParser.Processes
{
    public interface ILifecycle
    {
        void Execute(string folderToProcess, bool generateLogOnOK);
    }
}