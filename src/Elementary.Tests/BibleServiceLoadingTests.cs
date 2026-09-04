using Elementary.Core.Enums;
using Elementary.Core.Interfaces;
using Elementary.Core.Models;
using Elementary.Core.Services;
using Moq;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elementary.Tests.Services
{
    [TestClass]
    public class BibleServiceLoadingTests
    {
        [TestMethod]
        public async Task GetBible_ShouldNotPreloadSavedBook_WhenLoadingUsfmTranslation()
        {
            // Arrange
            var settingsServiceMock = new Mock<ISettingsService>(MockBehavior.Strict);
            var fileServiceMock = new Mock<IFileService>(MockBehavior.Strict);
            var filePathProviderMock = new Mock<IFilePathProvider>(MockBehavior.Strict);

            const string translationPath = "C:\\bibles\\net";
            filePathProviderMock
                .Setup(x => x.GetPathForTranslation(ETranslation.NET))
                .Returns(translationPath);

            fileServiceMock
                .Setup(x => x.ListFilesAsync(translationPath, "*.usfm"))
                .ReturnsAsync(new[]
                {
                    "C:\\bibles\\net\\01-GEN.usfm",
                    "C:\\bibles\\net\\02-EXO.usfm"
                });

            fileServiceMock
                .Setup(x => x.ReadFileAsync(It.IsAny<string>()))
                .Returns<string>(path =>
                {
                    var title = path.Contains("EXO") ? "Exodus" : "Genesis";
                    var usfm = $"\\\\id {title}\n\\\\h {title}\n\\\\c 1\n\\\\v 1 Sample text";
                    Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(usfm));
                    return Task.FromResult(stream);
                });

            var bibleService = new BibleService(settingsServiceMock.Object, fileServiceMock.Object, filePathProviderMock.Object);

            // Act
            var bible = await bibleService.GetBible(ETranslation.NET);

            // Assert
            Assert.AreEqual(2, bible.Books.Count);
            Assert.IsTrue(bible.Books.All(book => !book.IsChaptersLoaded));
            Assert.AreEqual("Genesis", bible.Books[0].Title);
            Assert.AreEqual(50, bible.Books[0].ChapterCount);
            Assert.AreEqual("Exodus", bible.Books[1].Title);
            Assert.AreEqual(40, bible.Books[1].ChapterCount);
            fileServiceMock.Verify(x => x.ReadFileAsync(It.IsAny<string>()), Times.Never);
            settingsServiceMock.Verify(x => x.GetSettings(), Times.Never);
        }

        [TestMethod]
        public async Task GetBible_ShouldFallBackToUsfmTitle_WhenFilenameMetadataIsUnavailable()
        {
            // Arrange
            var settingsServiceMock = new Mock<ISettingsService>(MockBehavior.Strict);
            var fileServiceMock = new Mock<IFileService>(MockBehavior.Strict);
            var filePathProviderMock = new Mock<IFilePathProvider>(MockBehavior.Strict);

            const string translationPath = "C:\\bibles\\net";
            filePathProviderMock
                .Setup(x => x.GetPathForTranslation(ETranslation.NET))
                .Returns(translationPath);

            fileServiceMock
                .Setup(x => x.ListFilesAsync(translationPath, "*.usfm"))
                .ReturnsAsync(new[]
                {
                    "C:\\bibles\\net\\custom-book.usfm"
                });

            fileServiceMock
                .Setup(x => x.ReadFileAsync("C:\\bibles\\net\\custom-book.usfm"))
                .Returns(() =>
                {
                    Stream stream = new MemoryStream(Encoding.UTF8.GetBytes("\\id Custom\n\\h Custom Book\n\\c 1\n\\v 1 Sample text"));
                    return Task.FromResult(stream);
                });

            var bibleService = new BibleService(settingsServiceMock.Object, fileServiceMock.Object, filePathProviderMock.Object);

            // Act
            var bible = await bibleService.GetBible(ETranslation.NET);

            // Assert
            Assert.AreEqual(1, bible.Books.Count);
            Assert.AreEqual("Custom Book", bible.Books[0].Title);
            Assert.AreEqual(0, bible.Books[0].ChapterCount);
            fileServiceMock.Verify(x => x.ReadFileAsync("C:\\bibles\\net\\custom-book.usfm"), Times.Once);
            settingsServiceMock.Verify(x => x.GetSettings(), Times.Never);
        }
    }
}
