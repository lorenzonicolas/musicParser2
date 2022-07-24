using musicParser.DTO;
using musicParser.Utils.Loggers;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;

namespace musicParser.Utils.FileSystemUtils
{
    public interface IFileSystemUtils
    {
        bool ValidateDirectory(string directory, bool createIfNotExists = false);
        IFileInfo GetAlbumCover(IDirectoryInfo cdFolder, IExecutionLogger logger);
        string GetAlbumType(string name);
        bool IsRootArtistsFolder(IDirectoryInfo folder);
        bool IsArtistFolder(IDirectoryInfo artistFolder);
        bool IsAlbumFolder(IDirectoryInfo albumFolder);
        bool AlbumContainsCDFolders(IDirectoryInfo folder);
        IFileInfo GetAnyFolderSong(IDirectoryInfo cdFolder);
        IFileInfo[] GetFolderSongs(IDirectoryInfo cdFolder);
        IFileInfo[] GetFolderImages(IDirectoryInfo cdFolder);
        IDirectoryInfo[] GetFolderAlbums(IDirectoryInfo mainFolder);
        IList<IDirectoryInfo> GetFolderAlbums(string path);
        IDirectoryInfo[] GetFolderArtists(IDirectoryInfo mainFolder);
        IDirectoryInfo[] GetFolderArtists(string mainFolder);
        bool IsFileLocked(string filename);
        void UnlockFile(string filename);
        string GetAlbumFolderName(IFileInfo file);
        FolderType GetFolderType(IDirectoryInfo folder);
        void SaveImageFile(IDirectoryInfo cdFolder, byte[] imageTagBytes);
        string MoveFolder(string source, string destination);
        string MoveProcessedFolder(string source, string destination);
        string CopyFolder(string source, string destination);
        bool IsAlbumNameCorrect(string albumName);

    }
}
