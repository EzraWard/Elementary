using Elementary.VerseOfTheDay.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Elementary.VerseOfTheDay.Services
{
    public class VotdCacheService : IVotdCacheService
    {
        private static readonly TimeSpan Ttl = TimeSpan.FromHours(24);

        private readonly Dictionary<string, (object Value, DateTime ExpiresAt)> _store
            = new Dictionary<string, (object, DateTime)>();

        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

        public async Task<T> GetOrFetchAsync<T>(string key, Func<Task<T>> factory) where T : class
        {
            await _lock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_store.TryGetValue(key, out var entry) && entry.ExpiresAt > DateTime.UtcNow)
                {
                    Debug.WriteLine($"[VotdCacheService] Cache hit for '{key}' (expires {entry.ExpiresAt:O}).");
                    return (T)entry.Value;
                }
            }
            finally
            {
                _lock.Release();
            }

            Debug.WriteLine($"[VotdCacheService] Cache miss for '{key}'. Fetching new value.");

            // Fetch outside the lock to avoid holding it during a network call
            var result = await factory().ConfigureAwait(false);

            await _lock.WaitAsync().ConfigureAwait(false);
            try
            {
                _store[key] = (result!, DateTime.UtcNow.Add(Ttl));
                Debug.WriteLine($"[VotdCacheService] Stored '{key}' in cache until {_store[key].ExpiresAt:O}.");
            }
            finally
            {
                _lock.Release();
            }

            return result;
        }

        public void Invalidate(string key)
        {
            _lock.Wait();
            try
            {
                var removed = _store.Remove(key);
                Debug.WriteLine($"[VotdCacheService] Invalidate '{key}' removed={removed}.");
            }
            finally { _lock.Release(); }
        }
    }
}
