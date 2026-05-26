using System;
using System.Threading.Tasks;

namespace Elementary.VerseOfTheDay.Interfaces
{
    public interface IVotdCacheService
    {
        Task<T> GetOrFetchAsync<T>(string key, Func<Task<T>> factory) where T : class;
        void Invalidate(string key);
    }
}
