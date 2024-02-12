using DTO;
using musicParser.Utils.Loggers;
using System.IO.Abstractions;

namespace musicParser.TagProcess
{
    public interface ITagsUtils
    {
        int? GetYear(IDirectoryInfo cdFolder, IExecutionLogger logger);
        bool IsValidYear(int year);
        bool IsInvalidGenre(string[] genres);
        string GetArtistFromTag(IDirectoryInfo folder, IExecutionLogger logger);
        string? GetAlbumFromTag(IDirectoryInfo folder, IExecutionLogger logger);
        string GetAlbumGenreFromTag(IDirectoryInfo albumFolder);
        string GetAlbumGenre(string fullPath);
        byte[]? GetCover(IFileInfo[] files);
        bool NamesAreDifferent(string str1, string str2);
        void UpdateFolderGenre(IDirectoryInfo folder);
        void UpdateAlbumTitleTags(IDirectoryInfo folder, string title, IExecutionLogger executionlogger);
        bool IsValidYear(string? yearFromMetalArchives);
        void UnlockFile(string fullname);
        SongInfo GetFileInformation(IFileInfo file);
    }
}