using Elementary.Core.Enums;
using Elementary.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Elementary.Core.Interfaces
{
    public interface ISearchService
    {
        Task<List<SearchResult>> SearchAsync(Bible bible, ETranslation translation, string query, ESearchScope scope);
    }
}
