using Elementary.VerseOfTheDay.Services;
using System;
using System.Threading.Tasks;

namespace Elementary.Tests.Services
{
    [TestClass]
    public class VotdCacheServiceTests
    {
        [TestMethod]
        public async Task GetOrFetchAsync_ShouldCallFactory_OnFirstCall()
        {
            var cache = new VotdCacheService();
            int callCount = 0;

            var result = await cache.GetOrFetchAsync("key1", () =>
            {
                callCount++;
                return Task.FromResult(new object());
            });

            Assert.IsNotNull(result);
            Assert.AreEqual(1, callCount);
        }

        [TestMethod]
        public async Task GetOrFetchAsync_ShouldReturnCachedValue_OnSecondCall()
        {
            var cache = new VotdCacheService();
            int callCount = 0;
            var sentinel = new object();

            await cache.GetOrFetchAsync("key2", () => { callCount++; return Task.FromResult(sentinel); });
            var second = await cache.GetOrFetchAsync("key2", () => { callCount++; return Task.FromResult(new object()); });

            Assert.AreEqual(1, callCount);
            Assert.AreSame(sentinel, second);
        }

        [TestMethod]
        public async Task GetOrFetchAsync_ShouldCallFactory_AfterInvalidate()
        {
            var cache = new VotdCacheService();
            int callCount = 0;

            await cache.GetOrFetchAsync("key3", () => { callCount++; return Task.FromResult(new object()); });
            cache.Invalidate("key3");
            await cache.GetOrFetchAsync("key3", () => { callCount++; return Task.FromResult(new object()); });

            Assert.AreEqual(2, callCount);
        }

        [TestMethod]
        public async Task GetOrFetchAsync_ShouldIsolateKeys()
        {
            var cache = new VotdCacheService();
            var obj1 = new object();
            var obj2 = new object();

            var r1 = await cache.GetOrFetchAsync("keyA", () => Task.FromResult(obj1));
            var r2 = await cache.GetOrFetchAsync("keyB", () => Task.FromResult(obj2));

            Assert.AreSame(obj1, r1);
            Assert.AreSame(obj2, r2);
        }
    }
}
