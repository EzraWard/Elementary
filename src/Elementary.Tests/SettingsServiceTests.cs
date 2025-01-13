using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Elementary.Core.Enums;
using Elementary.Core.Interfaces;
using Elementary.Core.Models;
using Elementary.Services;

namespace Elementary.Tests.Services
{
    [TestClass]
    public class SettingsServiceTests
    {
        private readonly Mock<ISettingsProvider> _mockSettingsProvider = new Mock<ISettingsProvider>();
        private readonly SettingsService _settingsService = new SettingsService();

        [TestInitialize]
        public void Setup()
        {
            _settingsService.SetSettingsProvider(_mockSettingsProvider.Object);
        }

        [TestMethod]
        public void GetSettings_ShouldParseValuesAndReturnCorrectSettings()
        {
            // Arrange
            _mockSettingsProvider.Setup(p => p.GetSetting("translation")).Returns(ETranslation.KJV.ToString());
            _mockSettingsProvider.Setup(p => p.GetSetting("book")).Returns(EBook.Psalms.ToString());
            _mockSettingsProvider.Setup(p => p.GetSetting("chapter")).Returns("5");
            _mockSettingsProvider.Setup(p => p.GetSetting("font")).Returns(EFont.SegoeUIVariable.ToString());
            _mockSettingsProvider.Setup(p => p.GetSetting("fontSize")).Returns(EFontSize.Large.ToString());
            _mockSettingsProvider.Setup(p => p.GetSetting("showVerseNumbers")).Returns("true");
            _mockSettingsProvider.Setup(p => p.GetSetting("theme")).Returns(ETheme.Dark.ToString());

            // Act
            var settings = _settingsService.GetSettings();

            // Assert
            Assert.AreEqual(ETranslation.KJV, settings.Translation);
            Assert.AreEqual(EBook.Psalms, settings.Book);
            Assert.AreEqual(5, settings.Chapter);
            Assert.AreEqual(EFont.SegoeUIVariable, settings.Font);
            Assert.AreEqual(EFontSize.Large, settings.FontSize);
            Assert.IsTrue(settings.ShowVerseNumbers);
            Assert.AreEqual(ETheme.Dark, settings.Theme);
        }

        [TestMethod]
        public void GetSettings_ShouldApplyDefaultValues_WhenSettingsAreNotPresent()
        {
            // Arrange
            _mockSettingsProvider.Setup(p => p.GetSetting(It.IsAny<string>())).Returns((string)null);

            // Act
            var settings = _settingsService.GetSettings();

            // Assert
            Assert.AreEqual(ETranslation.NET, settings.Translation); // Default translation
            Assert.AreEqual(EBook.Genesis, settings.Book); // Default book
            Assert.AreEqual(1, settings.Chapter); // Default chapter
            Assert.AreEqual(EFont.SegoeUIVariable, settings.Font); // Default font
            Assert.AreEqual(EFontSize.Medium, settings.FontSize); // Default font size
            Assert.IsTrue(settings.ShowVerseNumbers); // Default showVerseNumbers
        }

        [TestMethod]
        public void SaveSettings_ShouldSaveCorrectValues()
        {
            // Arrange
            var settings = new AppSettings
            {
                Translation = ETranslation.ASV,
                Book = EBook.Matthew,
                Chapter = 3,
                Font = EFont.SegoeUIVariable,
                FontSize = EFontSize.Small,
                ShowVerseNumbers = false,
                Theme = ETheme.Light
            };

            // Act
            _settingsService.SaveSettings(settings);

            // Assert
            _mockSettingsProvider.Verify(p => p.SaveSetting("translation", ETranslation.ASV.ToString()), Times.Once);
            _mockSettingsProvider.Verify(p => p.SaveSetting("book", EBook.Matthew.ToString()), Times.Once);
            _mockSettingsProvider.Verify(p => p.SaveSetting("chapter", "3"), Times.Once);
            _mockSettingsProvider.Verify(p => p.SaveSetting("font", EFont.SegoeUIVariable.ToString()), Times.Once);
            _mockSettingsProvider.Verify(p => p.SaveSetting("fontSize", EFontSize.Small.ToString()), Times.Once);
            _mockSettingsProvider.Verify(p => p.SaveSetting("showVerseNumbers", "false"), Times.Once);
            _mockSettingsProvider.Verify(p => p.SaveSetting("theme", ETheme.Light.ToString()), Times.Once);
        }

        [TestMethod]
        public void EnsureInitialization_ShouldSetDefaults_WhenValuesAreNotSet()
        {
            // Arrange
            var settings = new AppSettings
            {
                Translation = ETranslation.NotSet,
                Book = EBook.NotSet,
                Chapter = 0,
                Font = EFont.NotSet,
                FontSize = EFontSize.NotSet,
                ShowVerseNumbers = null,
                Theme = ETheme.NotSet
            };

            // Act
            _settingsService.SaveSettings(settings); // This will call EnsureInitialization internally

            // Assert
            Assert.AreEqual(ETranslation.NET, settings.Translation); // Default translation
            Assert.AreEqual(EBook.Genesis, settings.Book); // Default book
            Assert.AreEqual(1, settings.Chapter); // Default chapter
            Assert.AreEqual(EFont.SegoeUIVariable, settings.Font); // Default font
            Assert.AreEqual(EFontSize.Medium, settings.FontSize); // Default font size
            Assert.IsTrue(settings.ShowVerseNumbers); // Default showVerseNumbers
        }
    }
}
