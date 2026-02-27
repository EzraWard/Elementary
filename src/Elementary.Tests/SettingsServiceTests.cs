using Elementary.Core.Enums;
using Elementary.Core.Interfaces;
using Elementary.Core.Models;
using Elementary.Core.Services;
using Moq;

namespace Elementary.Core.Tests.Services
{
    [TestClass]
    public class SettingsServiceTests
    {
        private Mock<ISettingsProvider> _settingsProviderMock;
        private SettingsService _settingsService;

        [TestInitialize]
        public void Setup()
        {
            _settingsProviderMock = new Mock<ISettingsProvider>();
            _settingsService = new SettingsService(_settingsProviderMock.Object);
        }

        [TestMethod]
        public void GetSettings_ShouldReturnExpectedSettings()
        {
            // Arrange
            var settingsDictionary = new Dictionary<string, string>
            {
                { "translation", ETranslation.NET.ToString() },
                { "book", EBook.Genesis.ToString() },
                { "chapter", "1" },
                { "font", EFont.SegoeUIVariable.ToString() },
                { "fontSize", EFontSize.Medium.ToString() },
                { "showVerseNumbers", "true" },
                { "theme", ETheme.Light.ToString() }
            };

            _settingsProviderMock.Setup(x => x.GetSetting(It.IsAny<string>()))
                .Returns<string>(key => settingsDictionary[key]);

            // Act
            var settings = _settingsService.GetSettings();

            // Assert
            Assert.AreEqual(ETranslation.NET, settings.Translation);
            Assert.AreEqual(EBook.Genesis, settings.Book);
            Assert.AreEqual(1, settings.Chapter);
            Assert.AreEqual(EFont.SegoeUIVariable, settings.Font);
            Assert.AreEqual(EFontSize.Medium, settings.FontSize);
            Assert.IsTrue(settings.ShowVerseNumbers);
            Assert.AreEqual(ETheme.Light, settings.Theme);
        }

        [TestMethod]
        public void SaveSettings_ShouldSaveAllSettings()
        {
            // Arrange
            var appSettings = new AppSettings
            {
                Translation = ETranslation.NET,
                Book = EBook.Genesis,
                Chapter = 1,
                Font = EFont.SegoeUIVariable,
                FontSize = EFontSize.Medium,
                ShowVerseNumbers = true,
                Theme = ETheme.Light
            };

            // Act
            _settingsService.SaveSettings(appSettings);

            // Assert
            _settingsProviderMock.Verify(x => x.SaveSetting("translation", ETranslation.NET.ToString()), Times.Once);
            _settingsProviderMock.Verify(x => x.SaveSetting("book", EBook.Genesis.ToString()), Times.Once);
            _settingsProviderMock.Verify(x => x.SaveSetting("chapter", "1"), Times.Once);
            _settingsProviderMock.Verify(x => x.SaveSetting("font", EFont.SegoeUIVariable.ToString()), Times.Once);
            _settingsProviderMock.Verify(x => x.SaveSetting("fontSize", EFontSize.Medium.ToString()), Times.Once);
            _settingsProviderMock.Verify(x => x.SaveSetting("showVerseNumbers", "True"), Times.Once);
            _settingsProviderMock.Verify(x => x.SaveSetting("theme", ETheme.Light.ToString()), Times.Once);
        }

        [TestMethod]
        public void GetSettings_ShouldInitializeDefaultsWhenValuesMissing()
        {
            // Arrange
            var settingsDictionary = new Dictionary<string, string>
            {
                { "translation", "" },
                { "book", "" },
                { "chapter", "0" },
                { "font", "" },
                { "fontSize", "" },
                { "showVerseNumbers", "" },
                { "theme", "" }
            };

            _settingsProviderMock.Setup(x => x.GetSetting(It.IsAny<string>()))
                .Returns<string>(key => settingsDictionary[key]);

            // Act
            var settings = _settingsService.GetSettings();

            // Assert
            Assert.AreEqual(ETranslation.NET, settings.Translation);
            Assert.AreEqual(EBook.Genesis, settings.Book);
            Assert.AreEqual(1, settings.Chapter);
            Assert.AreEqual(EFont.SegoeUIVariable, settings.Font);
            Assert.AreEqual(EFontSize.Medium, settings.FontSize);
            Assert.IsTrue(settings.ShowVerseNumbers);
        }

        [TestMethod]
        public void SaveNavigationHistory_ShouldTrimToLastTenItems()
        {
            // Arrange
            var history = Enumerable.Range(1, 12)
                .Select(i => new NavigationHistoryItem { BookTitle = $"Book{i}", Chapter = i })
                .ToList();

            // Act
            _settingsService.SaveNavigationHistory(history);

            // Assert
            _settingsProviderMock.Verify(x => x.SaveSetting(
                "navigationHistory",
                "Book3|3;Book4|4;Book5|5;Book6|6;Book7|7;Book8|8;Book9|9;Book10|10;Book11|11;Book12|12"), Times.Once);
        }

        [TestMethod]
        public void GetNavigationHistory_ShouldParseValidEntriesAndIgnoreInvalidRows()
        {
            // Arrange
            _settingsProviderMock
                .Setup(x => x.GetSetting("navigationHistory"))
                .Returns("Genesis|1;InvalidOnly;Exodus|NaN;John|3");

            // Act
            var history = _settingsService.GetNavigationHistory();

            // Assert
            Assert.AreEqual(2, history.Count);
            Assert.AreEqual("Genesis", history[0].BookTitle);
            Assert.AreEqual(1, history[0].Chapter);
            Assert.AreEqual("John", history[1].BookTitle);
            Assert.AreEqual(3, history[1].Chapter);
        }
    }
}
