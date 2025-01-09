using Elementary.Core.Enums;
using Elementary.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                    return "ms-appx:///Content/NET21NOTELESS.epub";
                default:
                    return null;
            }
        }
    }
}
