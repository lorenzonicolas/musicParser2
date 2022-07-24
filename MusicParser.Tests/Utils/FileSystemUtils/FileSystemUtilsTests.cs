using Moq;
using musicParser.DTO;
using musicParser.Utils.Loggers;
using musicParser.Utils.Regex;
using System.IO.Abstractions;
using static musicParser.Utils.FileSystemUtils.FileSystemUtils;

namespace MusicParser.Tests.Utils.FileSystemUtils
{
    [TestFixture]
    public class FileSystemUtilsTests
    {
        private Mock<IConsoleLogger> consoleLogger = new();
        private Mock<IExecutionLogger> executionLogger = new();
        private Mock<IRegexUtils> regexUtils = new();
        private Mock<IFileSystem> fs = new();
        private Mock<IDirectory> directory = new();
        private Mock<IDirectoryInfoFactory> directoryInfo = new();
        private Mock<IFileInfoFactory> fileInfoFactory = new();
        private Mock<IFile> file = new();
        private musicParser.Utils.FileSystemUtils.FileSystemUtils utils;

        private Mock<IDirectoryInfo> normalAlbum = new();
        private Mock<IDirectoryInfo> albumWithWeirdImageFileName = new();
        private Mock<IDirectoryInfo> albumWithInnerFolders = new();
        private Mock<IDirectoryInfo> bandFolder = new();
        private Mock<IDirectoryInfo> rootBandsFolder = new();

        [OneTimeSetUp]
        public void Setup()
        {
            fs.Setup(f => f.Directory).Returns(directory.Object);
            fs.Setup(f => f.DirectoryInfo).Returns(directoryInfo.Object);
            fs.Setup(f => f.FileInfo).Returns(fileInfoFactory.Object);
            fs.Setup(f => f.File).Returns(file.Object);

            this.utils = new musicParser.Utils.FileSystemUtils.FileSystemUtils(consoleLogger.Object, regexUtils.Object, fs.Object);

            // Setup a normal album folder with songs, an image and a txt
            normalAlbum = CreateNormalAlbum();

            // Setup an album folder with a weird image name instead of FRONT.jpg
            var files = BuildAlbumFiles_WeirdImage();
            albumWithWeirdImageFileName.Setup(mock => mock.GetFiles()).Returns(files.ToArray());
            albumWithWeirdImageFileName.Setup(mock => mock.Name).Returns("1994 - In the Nightside Eclipse");

            // Setup an album folder with inner cd folders: albumname/cd1/songs etc.
            var cd1 = CreateNormalAlbum();
            cd1.Setup(mock => mock.Name).Returns("CD1");
            cd1.Setup(mock => mock.Parent).Returns(albumWithInnerFolders.Object);
            var cd2 = CreateNormalAlbum();
            cd2.Setup(mock => mock.Name).Returns("CD2");
            cd2.Setup(mock => mock.Parent).Returns(albumWithInnerFolders.Object);
            var innerCDs = new List<IDirectoryInfo> { cd1.Object, cd2.Object };
            albumWithInnerFolders.Setup(mock => mock.EnumerateDirectories()).Returns(innerCDs);
            albumWithInnerFolders.Setup(mock => mock.GetDirectories()).Returns(innerCDs.ToArray());
            albumWithInnerFolders.Setup(mock => mock.Name).Returns("1994 - In the Nightside Eclipse");

            // Setup a normal band folder with 1 normal album
            bandFolder.Setup(mock => mock.EnumerateDirectories())
                .Returns(new List<IDirectoryInfo> { normalAlbum.Object });
            bandFolder.Setup(mock => mock.GetDirectories())
                .Returns(new List<IDirectoryInfo> { normalAlbum.Object }.ToArray());
            bandFolder.Setup(mock => mock.Name).Returns("Emperor");

            // Setup a root artists folder - with only Emperor as band
            rootBandsFolder.Setup(mock => mock.EnumerateDirectories())
                .Returns(new List<IDirectoryInfo> { bandFolder.Object });
            rootBandsFolder.Setup(mock => mock.GetDirectories())
                .Returns(new List<IDirectoryInfo> { bandFolder.Object }.ToArray());
        }

        private Mock<IDirectoryInfo> CreateNormalAlbum()
        {
            var mockedFiles = BuildAlbumFolderFiles();
            var albumMock = new Mock<IDirectoryInfo>();
            albumMock.Setup(mock => mock.GetFiles()).Returns(mockedFiles.Select(x=>x.Object).ToArray());
            albumMock.Setup(mock => mock.Name).Returns("1994 - In the Nightside Eclipse");
            foreach (var file in mockedFiles)
            {
                file.Setup(x => x.Directory).Returns(albumMock.Object);
            }
            return albumMock;
        }

        [Test]
        [TestCase("FRONT.JPG", true)]
        [TestCase("FOLDER.JPG", true)]
        [TestCase("cover.JPG", false)]
        [Parallelizable(ParallelScope.All)]
        public void IsAlbumNameCorrect(string albumName, bool isCorrect)
        {
            Assert.That(utils.IsAlbumNameCorrect(albumName), Is.EqualTo(isCorrect));
        }

        [Test]
        [TestCase("Some Album Name (EP)", "EP")]
        [TestCase("Some Album Name [EP]", "EP")]
        [TestCase("Some Album Name (Demo)", "Demo")]
        [TestCase("Some Album Name [Demo]", "Demo")]
        [TestCase("Some Album Name (Single)", "Single")]
        [TestCase("Some Album Name [Single]", "Single")]
        [TestCase("Some Album Name (Split)", "Split")]
        [TestCase("Some Album Name [Split]", "Split")]
        [TestCase("Some Album Name", "FullAlbum")]
        [Parallelizable(ParallelScope.All)]
        public void GetAlbumType(string algo, string expected)
        {
            var result = utils.GetAlbumType(algo);

            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        [TestCase(FolderTestType.NormalAlbum, 2)]
        [TestCase(FolderTestType.AlbumWithInnerCds, 0)]
        [TestCase(FolderTestType.BandFolder, 0)]
        [TestCase(FolderTestType.RootBandsFolder, 0)]
        public void GetFolderSongs(FolderTestType folderType, int expected)
        {
            var result = utils.GetFolderSongs(GetObjectToUse(folderType));

            Assert.That(result, Has.Length.EqualTo(expected));
        }

        [Test]
        [TestCase(FolderTestType.NormalAlbum, true)]
        [TestCase(FolderTestType.AlbumWithInnerCds, true)]
        [TestCase(FolderTestType.BandFolder, false)]
        [TestCase(FolderTestType.RootBandsFolder, false)]
        public void IsAlbumFolder(FolderTestType folderType, bool expected)
        {
            var result = utils.IsAlbumFolder(GetObjectToUse(folderType));
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        [TestCase(FolderTestType.NormalAlbum, false)]
        [TestCase(FolderTestType.AlbumWithInnerCds, false)]
        [TestCase(FolderTestType.BandFolder, false)]
        [TestCase(FolderTestType.RootBandsFolder, true)]
        public void IsRootArtistsFolder(FolderTestType folderType, bool expected)
        {
            var result = utils.IsRootArtistsFolder(GetObjectToUse(folderType));
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        [TestCase(FolderTestType.NormalAlbum, false)]
        [TestCase(FolderTestType.AlbumWithInnerCds, true)]
        [TestCase(FolderTestType.BandFolder, false)]
        [TestCase(FolderTestType.RootBandsFolder, false)]
        public void AlbumContainCDFolders(FolderTestType folderType, bool expected)
        {
            var result = utils.AlbumContainsCDFolders(GetObjectToUse(folderType));
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        [TestCase(FolderTestType.NormalAlbum, false)]
        [TestCase(FolderTestType.AlbumWithInnerCds, false)]
        [TestCase(FolderTestType.BandFolder, true)]
        [TestCase(FolderTestType.RootBandsFolder, true)]
        public void GetAnyFolderSong(FolderTestType folderType, bool resultIsNull)
        {
            var result = utils.GetAnyFolderSong(GetObjectToUse(folderType));
            Assert.That(result is null, Is.EqualTo(resultIsNull));
        }

        [Test]
        [TestCase(FolderTestType.NormalAlbum, 4)]
        [TestCase(FolderTestType.AlbumWithInnerCds, 0)]
        [TestCase(FolderTestType.BandFolder, 0)]
        [TestCase(FolderTestType.RootBandsFolder, 0)]
        public void GetFolderImages(FolderTestType folderType, int expected)
        {
            var result = utils.GetFolderImages(GetObjectToUse(folderType));

            Assert.That(result, Has.Length.EqualTo(expected));
        }

        [Test]
        [TestCase(FolderTestType.NormalAlbum, false)]
        [TestCase(FolderTestType.AlbumWithInnerCds, false)]
        [TestCase(FolderTestType.BandFolder, true)]
        [TestCase(FolderTestType.RootBandsFolder, false)]
        public void IsArtistFolder (FolderTestType folderType, bool expected)
        {
            var result = utils.IsArtistFolder(GetObjectToUse(folderType));
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        [TestCase(FolderTestType.NormalAlbum, "FRONT.jpg")]
        [TestCase(FolderTestType.AlbumWithWeirdImage, "weirdName.jpg")]
        [TestCase(FolderTestType.AlbumWithInnerCds, null)]
        [TestCase(FolderTestType.BandFolder, null)]
        [TestCase(FolderTestType.RootBandsFolder, null)]
        public void GetAlbumCover(FolderTestType folderType, string expectedFileName)
        {
            var result = utils.GetAlbumCover(GetObjectToUse(folderType), executionLogger.Object);
            Assert.That(result?.Name, Is.EqualTo(expectedFileName));
        }

        [Test]
        [TestCase(FolderTestType.NormalAlbum, FolderType.Album)]
        [TestCase(FolderTestType.AlbumWithWeirdImage, FolderType.Album)]
        [TestCase(FolderTestType.AlbumWithInnerCds, FolderType.AlbumWithMultipleCDs)]
        [TestCase(FolderTestType.BandFolder, FolderType.ArtistWithAlbums)]
        [TestCase(FolderTestType.RootBandsFolder, FolderType.Album, true)]
        public void GetFolderType(FolderTestType folderType, FolderType expected, bool throwsEx = false)
        {
            if(throwsEx)
                Assert.Throws<FolderTypeException>(() => utils.GetFolderType(GetObjectToUse(folderType)));
            else
            {
                Assert.That(utils.GetFolderType(GetObjectToUse(folderType)), Is.EqualTo(expected));
            }                
        }

        [Test]
        [TestCase(FolderTestType.NormalAlbum, 0)]
        [TestCase(FolderTestType.AlbumWithInnerCds, 2)]
        [TestCase(FolderTestType.BandFolder, 1)]
        [TestCase(FolderTestType.RootBandsFolder, 0)]
        public void GetFolderAlbums(FolderTestType folderType, int expected)
        {
            var result = utils.GetFolderAlbums(GetObjectToUse(folderType));
            Assert.That(result?.Length, Is.EqualTo(expected));
        }

        [Test]
        [TestCase(FolderTestType.NormalAlbum, 0)]
        [TestCase(FolderTestType.AlbumWithInnerCds, 2)]
        [TestCase(FolderTestType.BandFolder, 1)]
        [TestCase(FolderTestType.RootBandsFolder, 0)]
        public void GetFolderAlbums_string(FolderTestType folderType, int expected)
        {
            var folderToUse = GetObjectToUse(folderType);
            directoryInfo.Setup(x => x.FromDirectoryName("path")).Returns(folderToUse);

            var result = utils.GetFolderAlbums("path");
            Assert.That(result?.Count, Is.EqualTo(expected));

            directoryInfo.Reset();
        }

        [Test]
        [TestCase(FolderTestType.NormalAlbum, 0)]
        [TestCase(FolderTestType.AlbumWithInnerCds, 0)]
        [TestCase(FolderTestType.BandFolder, 0)]
        [TestCase(FolderTestType.RootBandsFolder, 1)]
        public void GetFolderArtists(FolderTestType folderType, int expected)
        {
            var result = utils.GetFolderArtists(GetObjectToUse(folderType));
            Assert.That(result?.Length, Is.EqualTo(expected));
        }

        [Test]
        [TestCase(FolderTestType.NormalAlbum, 0)]
        [TestCase(FolderTestType.AlbumWithInnerCds, 0)]
        [TestCase(FolderTestType.BandFolder, 0)]
        [TestCase(FolderTestType.RootBandsFolder, 1)]
        public void GetFolderArtists_string(FolderTestType folderType, int expected)
        {
            var folderToUse = GetObjectToUse(folderType);
            directoryInfo.Setup(x => x.FromDirectoryName("path")).Returns(folderToUse);

            var result = utils.GetFolderArtists("path");
            Assert.That(result?.Length, Is.EqualTo(expected));

            directoryInfo.Reset();
        }

        [Test]
        [TestCase(FolderTestType.NormalAlbum)]
        [TestCase(FolderTestType.AlbumWithInnerCds)]
        public void GetAlbumFolderName(FolderTestType folderType)
        {
            var folderToUse = GetObjectToUse(folderType);

            regexUtils.Setup(x => x.GetFolderInformation(It.IsAny<string>()))
                .Returns(new FolderInfo() { Album = "test"});

            var songFile = utils.GetAnyFolderSong(folderToUse);
            var result = utils.GetAlbumFolderName(songFile);

            Assert.That(result, Is.EqualTo("test"));
            regexUtils.Verify(x => x.GetFolderInformation("1994 - In the Nightside Eclipse"), Times.Once);

            regexUtils.Reset();
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void ValidateDirectory(bool createIfNotExists)
        {
            this.directory.Setup(d=>d.Exists("testDirectoryPath"))
                .Returns(true);

            if(createIfNotExists)
                this.directory.Setup(d => d.CreateDirectory("testDirectoryPath"))
                    .Returns(new Mock<IDirectoryInfo>().Object);

            var result = utils.ValidateDirectory("testDirectoryPath", createIfNotExists);

            Assert.That(result, Is.True);

            this.directory.Reset();
        }

        [Test]
        public void ValidateDirectory_Exception()
        {
            this.directory.Setup(d => d.CreateDirectory("testDirectoryPath"))
                .Throws(new Exception("exceptionMsg"));

            var result = utils.ValidateDirectory("testDirectoryPath", true);

            Assert.That(result, Is.False);
            consoleLogger.Verify(mock => mock.Log("exceptionMsg", LogType.Error), Times.Once);

            this.directory.Reset();
        }

        [TestCase(true)]
        [TestCase(false)]
        public void UnlockFile(bool isReadonlyAlready)
        {
            var file1 = new Mock<IFileInfo>();
            file1.Setup(f => f.IsReadOnly).Returns(isReadonlyAlready);
            fileInfoFactory.Setup(f => f.FromFileName("fileName")).Returns(file1.Object);

            utils.UnlockFile("fileName");

            if (isReadonlyAlready)
                file1.VerifySet(x => x.IsReadOnly = false, Times.Once);
            else
                file1.VerifySet(x => x.IsReadOnly = false, Times.Never);

            fileInfoFactory.Reset();            
        }

        [Test]
        [TestCase("C:\\Music\\Emperor", "C:\\Destiny", "C:\\Destiny\\Emperor")]
        [TestCase("C:\\Music\\Emperor\\1994 - In", "C:\\Destiny", "C:\\Destiny\\1994 - In")]
        public void MoveFolder(string source, string destiny, string expectedDestiny)
        {
            var mockedDir = new Mock<IDirectoryInfo>();
            directoryInfo.Setup(x => x.FromDirectoryName(source)).Returns(mockedDir.Object);

            var result = utils.MoveFolder(source, destiny);

            Assert.That(result, Is.EqualTo(expectedDestiny));
            mockedDir.Verify(x => x.MoveTo(expectedDestiny), Times.Once);

            directoryInfo.Reset();
        }

        [Test]
        [TestCase("C:\\Music\\Emperor\\1994 - In", "C:\\Destiny\\Emperor", "C:\\Destiny\\Emperor\\1994 - In")]
        public void CopyFolder(string source, string destiny, string expectedDestiny)
        {
            var mockedDir = new Mock<IDirectoryInfo>();
            mockedDir.Setup(x => x.FullName).Returns(source);
            directoryInfo.Setup(x => x.FromDirectoryName(source)).Returns(mockedDir.Object);
            directory.Setup(x => x.Exists(expectedDestiny)).Returns(true);
            mockedDir.Setup(x => x.GetDirectories()).Returns(new List<IDirectoryInfo>().ToArray());

            var mockedFiles = BuildAlbumFolderFiles();
            foreach (var file in mockedFiles)
            {
                file.Setup(x => x.FullName).Returns($"{source}\\{file.Object.Name}");
            }
            mockedDir.Setup(x => x.GetFiles()).Returns(mockedFiles.Select(x => x.Object).ToArray());

            var result = utils.CopyFolder(source, destiny);

            Assert.That(result, Is.EqualTo(expectedDestiny));
            directory.Verify(x => x.CreateDirectory(It.IsAny<string>()), Times.Never);

            foreach (var item in mockedFiles)
            {
                file.Verify(x => x.Copy(item.Object.FullName, $"{expectedDestiny}\\{item.Object.Name}", true), Times.Once);
            }

            file.Reset();
            directoryInfo.Reset();
            directory.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            consoleLogger.Reset();
        }

        #region Test Utils

        private IDirectoryInfo GetObjectToUse(FolderTestType value)
        {
            switch (value)
            {
                case FolderTestType.NormalAlbum:
                    return normalAlbum.Object;
                case FolderTestType.AlbumWithInnerCds:
                    return albumWithInnerFolders.Object;
                case FolderTestType.BandFolder:
                    return bandFolder.Object;
                case FolderTestType.RootBandsFolder:
                    return rootBandsFolder.Object;
                case FolderTestType.AlbumWithWeirdImage:
                    return albumWithWeirdImageFileName.Object;
            }

            return normalAlbum.Object;
        }

        public enum FolderTestType
        {
            NormalAlbum,
            AlbumWithInnerCds,
            BandFolder,
            RootBandsFolder,
            AlbumWithWeirdImage
        }

        private Mock<IDirectoryInfo> BuildMockedDirectoryFromJSON(string json)
        {
            var mockExplanation = Newtonsoft.Json.JsonConvert.DeserializeObject<MockFolder>(json);

            return BuildDirectory(mockExplanation);
        }

        private Mock<IDirectoryInfo> BuildDirectory(MockFolder info, IDirectoryInfo? parentFolder = null)
        {
            var directory = new Mock<IDirectoryInfo>();

            directory.Setup(x => x.Exists).Returns(true);
            directory.Setup(x => x.Name).Returns(info.FolderName);

            var mockedFiles = info.Files.Select(x => BuildFile(x).Object).ToArray();
            directory.Setup(x => x.GetFiles()).Returns(mockedFiles);

            // Parent folder setup
            var parent = parentFolder ?? BuildDirectory(info.FolderParent).Object;
            directory.Setup(x => x.Parent).Returns(parent);

            // Child folders setup
            var childFolders = info.ChildFolders.Select(x => BuildDirectory(x, directory.Object).Object).ToArray();
            directory.Setup(x => x.GetDirectories()).Returns(childFolders);

            return directory;
        }

        private Mock<IFileInfo> BuildFile(MockFile info)
        {
            var file = new Mock<IFileInfo>();
            file.Setup(x => x.Exists).Returns(true);
            file.Setup(x => x.Extension).Returns(info.FileName.Split('.').Last());
            file.Setup(x => x.Name).Returns(info.FileName);
            file.Setup(x => x.FullName).Returns(info.FileFullName);
            file.Setup(x => x.DirectoryName).Returns(info.FileFullName);

            return file;
        }

        private class MockFolder
        {
            public string FolderName { get; set; }
            public MockFolder FolderParent { get; set; }
            public IList<MockFolder> ChildFolders { get; set; }
            public IList<MockFile> Files { get; set; }
        }

        private class MockFile
        {
            public string FileName { get; set; }
            public string FileFullName { get; set; }
        }

        private List<IFileInfo> BuildAlbumFiles_WeirdImage()
        {
            var file1 = new Mock<IFileInfo>();
            file1.Setup(f => f.Name).Returns("file1.mp3");
            file1.Setup(f => f.Extension).Returns(".mp3");

            var file31 = new Mock<IFileInfo>();
            file31.Setup(f => f.Name).Returns("weirdName.jpg");
            file31.Setup(f => f.Extension).Returns(".jpg");

            var fileList = new List<IFileInfo>
            {
                file1.Object, file31.Object
            };
            return fileList;
        }

        private List<Mock<IFileInfo>> BuildAlbumFolderFiles()
        {
            var file1 = new Mock<IFileInfo>();
            file1.Setup(f => f.Name).Returns("file1.mp3");
            file1.Setup(f => f.Extension).Returns(".mp3");

            var file2 = new Mock<IFileInfo>();
            file2.Setup(f => f.Name).Returns("file2.wav");
            file2.Setup(f => f.Extension).Returns(".wav");

            var file31 = new Mock<IFileInfo>();
            file31.Setup(f => f.Name).Returns("FRONT.jpg");
            file31.Setup(f => f.Extension).Returns(".jpg");

            var file32 = new Mock<IFileInfo>();
            file32.Setup(f => f.Name).Returns("file32.jpe");
            file32.Setup(f => f.Extension).Returns(".jpe");

            var file33 = new Mock<IFileInfo>();
            file33.Setup(f => f.Name).Returns("file33.bmp");
            file33.Setup(f => f.Extension).Returns(".bmp");

            var file34 = new Mock<IFileInfo>();
            file34.Setup(f => f.Name).Returns("file34.png");
            file34.Setup(f => f.Extension).Returns(".png");

            var filex = new Mock<IFileInfo>();
            filex.Setup(f => f.Name).Returns("file3.txt");
            filex.Setup(f => f.Extension).Returns(".txt");

            var fileList = new List<Mock<IFileInfo>>
            {
                file1, file2, file31, file32, file33, file34, filex
            };
            return fileList;
        }

        #endregion
    }
}
