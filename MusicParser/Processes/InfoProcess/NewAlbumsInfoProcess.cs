namespace musicParser.Processes.InfoProcess
{
    public interface INewAlbumsInfoProcess
    {
        object Execute(string folderPath);
    }
    public class NewAlbumsInfoProcess : INewAlbumsInfoProcess
    {
        private readonly IInfoActions infoActions;

        public NewAlbumsInfoProcess(IInfoActions info)
        {
            infoActions = info;
        }

        public object Execute(string folderPath)
        {
            infoActions.Sync(folderPath, updateDeletes: false);
            return string.Empty;
        }
    }
}
