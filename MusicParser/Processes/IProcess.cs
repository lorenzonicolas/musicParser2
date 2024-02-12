using System.Threading.Tasks;

namespace musicParser.Processes
{
    public interface IProcess
    {
        object Execute(string folderToProcess);
    }

    public interface IProcessAsync
    {
        Task<object> Execute();
    }
}
