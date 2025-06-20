using Elementary.Core.Enums;
using Elementary.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Elementary.Tests.UWP.Providers
{
    [TestClass]
    public class WindowsFilePathProviderTests
    {
        private WindowsFilePathProvider _provider;

        [TestInitialize]
        public void Setup()
        {
            _provider = new WindowsFilePathProvider();
        }

        [TestMethod]
        public void GetPathForTranslation_ShouldReturnCorrectPath_ForKnownTranslation()
        {
            // Act
            var path = _provider.GetPathForTranslation(ETranslation.ASV);

            // Assert
            Assert.IsNotNull(path);

            // Act
            path = null;
            path = _provider.GetPathForTranslation(ETranslation.KJV);

            // Assert
            Assert.IsNotNull(path);

            // Act
            path = null;
            path = _provider.GetPathForTranslation(ETranslation.NET);

            // Assert
            Assert.IsNotNull(path);
        }

        [TestMethod]
        public void GetPathForTranslation_ShouldReturnNull_ForUnknownTranslation()
        {
            // Act
            var path = _provider.GetPathForTranslation((ETranslation)999);

            // Assert
            Assert.IsNull(path);
        }
    }
}