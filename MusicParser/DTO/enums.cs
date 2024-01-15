namespace musicParser.DTO
{
    enum Options
    {
        ParseFolders = 1,
        ParseFiles = 2,
        CheckTags = 3,
        NewAlbumsMetadata = 4,
        FullLibraryScan = 5,
        Lifecycle = 6,
        Exit
    }

    public enum DirectorySide
    {
        MainInput,
        MainOutput
    }

    public enum FolderOperation
    {
        Move,
        Copy
    }

    public enum LogType
    {
        Error,
        Information,
        Process,
        Success,
        Warning
    }
}
