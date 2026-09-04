using Elementary.Core.Enums;

namespace Elementary.Core.Interfaces
{
    public interface IFilePathProvider
    {        
        string GetPathForTranslation(ETranslation translation);
    }
}
