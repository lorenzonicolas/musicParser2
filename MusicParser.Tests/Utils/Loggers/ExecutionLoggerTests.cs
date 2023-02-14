using Moq;
using musicParser.Utils.Loggers;
using System.IO.Abstractions;
using System.Text;

namespace MusicParser.Tests.Utils.Loggers
{
    public class ExecutionLoggerTests
    {
        readonly Mock<IFileSystem> fs = new();

        [TestCase]
        public void Constructor()
        {
            var logger = new ExecutionLogger(fs.Object);

            Assert.Multiple(() =>
            {
                Assert.That(logger.hasError, Is.False);
                Assert.That(logger.hasTagChangesNeeded, Is.False);
                Assert.That(logger.log, Is.Empty);
            });
        }

        [TestCase]
        public void StartExecutionLog()
        {
            var logger = new ExecutionLogger(fs.Object);

            logger.StartExecutionLog();

            Assert.Multiple(() =>
            {
                Assert.That(logger.hasError, Is.False);
                Assert.That(logger.hasTagChangesNeeded, Is.True);
                Assert.That(logger.log, Is.Empty);
            });
        }

        [TestCase]
        public void Log()
        {
            var logger = new ExecutionLogger(fs.Object);

            logger.Log("new");

            Assert.Multiple(() =>
            {
                Assert.That(logger.hasError, Is.False);
                Assert.That(logger.hasTagChangesNeeded, Is.False);
                Assert.That(logger.log, Does.Contain("new"));
            });
        }

        [TestCase]
        public void LogError()
        {
            var logger = new ExecutionLogger(fs.Object);

            logger.LogError("error-string");

            Assert.Multiple(() =>
            {
                Assert.That(logger.hasError, Is.True);
                Assert.That(logger.hasTagChangesNeeded, Is.False);
                Assert.That(logger.log, Does.Contain("error-string"));
            });
        }

        [TestCase]
        public void LogErrorException()
        {
            var logger = new ExecutionLogger(fs.Object);

            logger.LogError(new Exception("error-message"));

            Assert.Multiple(() =>
            {
                Assert.That(logger.hasError, Is.True);
                Assert.That(logger.hasTagChangesNeeded, Is.False);
                Assert.That(logger.log, Does.Contain("error-message"));
            });
        }

        [TestCase]
        public void LogTagNote()
        {
            var logger = new ExecutionLogger(fs.Object);

            logger.LogTagNote("tagNote");

            Assert.Multiple(() =>
            {
                Assert.That(logger.hasError, Is.False);
                Assert.That(logger.hasTagChangesNeeded, Is.True);
                Assert.That(logger.log, Does.Contain("tagNote"));
            });
        }

        [TestCase]
        public void ExportLogFile_ShouldNotGenerate()
        {
            var logger = new ExecutionLogger(fs.Object);

            logger.ExportLogFile("destiny", false);

            fs.Verify(o => o.Directory.Exists("destiny"), Times.Never);

            fs.Reset();
        }

        [TestCase]
        public void ExportLogFile_Exception()
        {
            var logger = new ExecutionLogger(fs.Object);
            fs.Setup(o => o.Directory.Exists("destiny")).Returns(false);

            Assert.Throws<Exception>(() => logger.ExportLogFile("destiny"));

            fs.Reset();
        }

        [TestCase("OK_executionLog.txt", false, false)]
        [TestCase("ERROR_executionLog.txt", true, false)]
        [TestCase("TAG_CHECK_executionLog", false, true)]
        public void ExportLogFile_Ok(string fileName, bool hasError, bool hasTagNote)
        {
            Mock<IFile> fileMock = new();
            
            var logger = new ExecutionLogger(fs.Object);

            logger.Log("someLog");

            if (hasError)
            {
                logger.LogError("error");
            }

            if (hasTagNote)
            {
                logger.LogTagNote("tagNote");
            }

            fs.Setup(fs => fs.File).Returns(fileMock.Object);
            fs.Setup(o => o.Directory.Exists("destiny")).Returns(true);
            fs.Setup(o => o.Path.Combine("destiny", It.Is<string>(x => x.Contains(fileName))))
                .Returns("path");

            // Act
            logger.ExportLogFile("destiny");

            // Assert
            fileMock.Verify(o => o.AppendAllText("path", It.Is<string>(x => x.Contains("someLog")), Encoding.UTF8));
            fs.Verify(o => o.Directory.Exists("destiny"), Times.Once);
            fs.Verify(o => o.Path.Combine("destiny", It.Is<string>(x => x.Contains(fileName))), Times.Once);

            fs.Reset();
        }
    }
}
