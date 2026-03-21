using Elementary.Core.Services;
using System.Text;

namespace Elementary.Tests.Services
{
    [TestClass]
    public class FileServiceTests : IDisposable
    {
        private FileService _fileService;
        private string _testDirectory;

        public FileServiceTests()
        {
            _fileService = new FileService();
            _testDirectory = Path.Combine(Path.GetTempPath(), "FileServiceTests");

            if (!Directory.Exists(_testDirectory))
            {
                Directory.CreateDirectory(_testDirectory);
            }
        }

        [TestMethod]
        public async Task WriteFileAsync_ShouldCreateFileWithCorrectContent()
        {
            // Arrange
            var filePath = Path.Combine(_testDirectory, "testfile.txt");
            var content = "Hello, world!";
            var contentStream = new MemoryStream(Encoding.UTF8.GetBytes(content));

            // Act
            await _fileService.WriteFileAsync(filePath, contentStream);

            // Assert
            Assert.IsTrue(File.Exists(filePath));
            var fileContent = await File.ReadAllTextAsync(filePath);
            Assert.AreEqual(content, fileContent);
        }

        [TestMethod]
        public async Task ReadFileAsync_ShouldReturnCorrectContent()
        {
            // Arrange
            var filePath = Path.Combine(_testDirectory, "readfile.txt");
            var expectedContent = "Read this content.";
            await File.WriteAllTextAsync(filePath, expectedContent);

            // Act
            var stream = await _fileService.ReadFileAsync(filePath);
            using var reader = new StreamReader(stream);
            var actualContent = await reader.ReadToEndAsync();

            // Assert
            Assert.AreEqual(expectedContent, actualContent);
        }

        [TestMethod]
        //[ExpectedException(typeof(FileNotFoundException))]
        public async Task ReadFileAsync_ShouldThrowIfFileDoesNotExist()
        {
            // Arrange
            var nonExistentPath = Path.Combine(_testDirectory, "nonexistent.txt");

            // Act & Assert
            try
            {
                await _fileService.ReadFileAsync(nonExistentPath);
                Assert.Fail("Expected FileNotFoundException was not thrown.");
            }
            catch (System.IO.FileNotFoundException)
            {
                // Expected
            }
        }

        [TestMethod]
        public async Task FileExistsAsync_ShouldReturnTrueIfFileExists()
        {
            // Arrange
            var filePath = Path.Combine(_testDirectory, "exists.txt");
            await File.WriteAllTextAsync(filePath, "Some content");

            // Act
            var exists = await _fileService.FileExistsAsync(filePath);

            // Assert
            Assert.IsTrue(exists);
        }

        [TestMethod]
        public async Task FileExistsAsync_ShouldReturnFalseIfFileDoesNotExist()
        {
            // Arrange
            var filePath = Path.Combine(_testDirectory, "doesnotexist.txt");

            // Act
            var exists = await _fileService.FileExistsAsync(filePath);

            // Assert
            Assert.IsFalse(exists);
        }

        [TestMethod]
        public async Task ListFilesAsync_ShouldReturnMatchingFiles()
        {
            // Arrange
            await File.WriteAllTextAsync(Path.Combine(_testDirectory, "one.usfm"), "a");
            await File.WriteAllTextAsync(Path.Combine(_testDirectory, "two.usfm"), "b");
            await File.WriteAllTextAsync(Path.Combine(_testDirectory, "three.txt"), "c");

            // Act
            var files = await _fileService.ListFilesAsync(_testDirectory, "*.usfm");

            // Assert
            var list = files.ToList();
            Assert.AreEqual(2, list.Count);
            Assert.IsTrue(list.Any(f => f.EndsWith("one.usfm")));
            Assert.IsTrue(list.Any(f => f.EndsWith("two.usfm")));
        }

        [TestMethod]
        public async Task ListFilesAsync_ShouldReturnEmptyWhenDirectoryDoesNotExist()
        {
            // Arrange
            var missingPath = Path.Combine(_testDirectory, "missing");

            // Act
            var files = await _fileService.ListFilesAsync(missingPath, "*.usfm");

            // Assert
            Assert.AreEqual(0, files.Count());
        }

        public void Dispose()
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
        }
    }
}
