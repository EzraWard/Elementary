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
                    return "ms-appx:///Content/ASV";
                case ETranslation.KJV:
                    return "ms-appx:///Content/KJV";
                case ETranslation.NET:
                    return "ms-appx:///Content/NET";
                default:
                    return null;
            }
        }
    }
}
