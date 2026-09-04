using Microsoft.VisualStudio.TestTools.UnitTesting;
using Elementary.Services;
using Windows.Storage;

namespace Elementary.Tests.UWP.Services
{
    [TestClass]
    public class WindowsSettingsProviderTests
    {
        private WindowsSettingsProvider _settingsProvider;

        [TestInitialize]
        public void Setup()
        {
            _settingsProvider = new WindowsSettingsProvider();
        }

        [TestMethod]
        public void SaveSetting_ShouldSaveValue()
        {
            // Arrange
            var key = "TestKey";
            var value = "TestValue";

            // Act
            _settingsProvider.SaveSetting(key, value);

            // Assert
            var savedValue = ApplicationData.Current.LocalSettings.Values[key];
            Assert.AreEqual(value, savedValue);
        }

        [TestMethod]
        public void GetSetting_ShouldReturnSavedValue()
        {
            // Arrange
            var key = "TestKey";
            var value = "TestValue";
            ApplicationData.Current.LocalSettings.Values[key] = value;

            // Act
            var result = _settingsProvider.GetSetting(key);

            // Assert
            Assert.AreEqual(value, result);
        }

        [TestMethod]
        public void GetSetting_ShouldReturnNull_WhenKeyDoesNotExist()
        {
            // Arrange
            var key = "NonExistentKey";

            // Act
            var result = _settingsProvider.GetSetting(key);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public void DeleteSetting_ShouldRemoveKey()
        {
            // Arrange
            var key = "TestKey";
            var value = "TestValue";
            ApplicationData.Current.LocalSettings.Values[key] = value;

            // Act
            _settingsProvider.DeleteSetting(key);

            // Assert
            Assert.IsFalse(ApplicationData.Current.LocalSettings.Values.ContainsKey(key));
        }

        [TestCleanup]
        public void Cleanup()
        {
            // Clean up any test keys
            ApplicationData.Current.LocalSettings.Values.Remove("TestKey");
            ApplicationData.Current.LocalSettings.Values.Remove("NonExistentKey");
        }
    }
}