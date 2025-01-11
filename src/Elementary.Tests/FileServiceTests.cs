using Elementary.Core.Services;
using System.Text;

namespace Elementary.Tests.Services
{
    [TestClass]
    public class FileServiceTests
    {
        private FileService _fileService = new FileService();

        [TestMethod]
        public void ReadFile_ShouldReturnStream_WhenFileExists()
        {
            // Arrange
            var path = "test.txt";
            var content = "File content";
            File.WriteAllText(path, content);

            try
            {
                // Act
                using var result = _fileService.ReadFile(path);

                // Assert
                using var reader = new StreamReader(result);
                Assert.AreEqual(content, reader.ReadToEnd());
            }
            finally
            {
                File.Delete(path); // Cleanup
            }
        }

        [TestMethod]
        [ExpectedException(typeof(FileNotFoundException))]
        public void ReadFile_ShouldThrowFileNotFoundException_WhenFileDoesNotExist()
        {
            // Arrange
            var invalidPath = "nonexistent.txt";

            // Act
            _fileService.ReadFile(invalidPath);
        }

        [TestMethod]
        public async Task WriteFileAsync_ShouldWriteContentToFile()
        {
            // Arrange
            var path = "output.txt";
            var content = "Hello, world!";
            var contentStream = new MemoryStream(Encoding.UTF8.GetBytes(content));

            try
            {
                // Act
                await _fileService.WriteFileAsync(path, contentStream);

                // Assert
                Assert.IsTrue(File.Exists(path));
                Assert.AreEqual(content, File.ReadAllText(path));
            }
            finally
            {
                File.Delete(path); // Cleanup
            }
        }

        [TestMethod]
        public async Task FileExistsAsync_ShouldReturnTrue_WhenFileExists()
        {
            // Arrange
            var path = "exists.txt";
            File.WriteAllText(path, "Test content");

            try
            {
                // Act
                var exists = await _fileService.FileExistsAsync(path);

                // Assert
                Assert.IsTrue(exists);
            }
            finally
            {
                File.Delete(path); // Cleanup
            }
        }

        [TestMethod]
        public async Task FileExistsAsync_ShouldReturnFalse_WhenFileDoesNotExist()
        {
            // Arrange
            var path = "nonexistent.txt";

            // Act
            var exists = await _fileService.FileExistsAsync(path);

            // Assert
            Assert.IsFalse(exists);
        }
    }
}