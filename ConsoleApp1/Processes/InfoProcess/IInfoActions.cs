namespace musicParser.Processes.InfoProcess
{
    public interface IInfoActions
    {
        void Sync(string folderPath, bool updateDeletes = false);
    }
}
