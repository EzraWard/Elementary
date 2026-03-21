using Elementary.Core.Enums;
using Elementary.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Elementary.Core.Interfaces
{
    public interface IBibleService
    {
       Task<Bible> GetBible(ETranslation translation);
       Task EnsureBookLoaded(ETranslation translation, Book book);
    }
}
