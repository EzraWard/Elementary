using Elementary.Core.Enums;
using Elementary.Core.Interfaces;

namespace Elementary.Services
{
    public class WindowsFilePathProvider: IFilePathProvider
    {
        public string GetPathForTranslation(ETranslation translation)
        {
            switch (translation)
            {
                case ETranslation.ASV:
                    return "ms-appx:///Content/eng-asv.epub";
                case ETranslation.KJV:
                    return "ms-appx:///Content/KJVNoImages.epub";
                case ETranslation.NET:
                    // Use the NET folder containing USFM files
                    return "ms-appx:///Content/NET";
                default:
                    return null;
            }
        }
    }
}
