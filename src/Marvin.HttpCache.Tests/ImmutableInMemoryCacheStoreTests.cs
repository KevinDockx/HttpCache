using Marvin.HttpCache.Store;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Marvin.HttpCache.Tests
{
    [TestClass]
    public class ImmutableInMemoryCacheStoreTests
    {

        [TestMethod]
        public async Task SetAndGetNew()
        {
            var store = new ImmutableInMemoryCacheStore();

            var resp = new HttpResponseMessage();

            var cacheKey = new CacheKey("key", null);
            await store.SetAsync(cacheKey, new CacheEntry(resp));

            var fromCache = await store.GetAsync(cacheKey);

            // check
            Assert.AreEqual(resp, fromCache.HttpResponse);
        }


        [TestMethod]
        public async Task SetAndGetTestCacheKeyEquality()
        {
            var store = new ImmutableInMemoryCacheStore();

            var resp = new HttpResponseMessage();

            var cacheKey = new CacheKey("key", null);
            await store.SetAsync(cacheKey, new CacheEntry(resp));

            var fromCache = await store.GetAsync(new CacheKey("key", null));

            // check
            Assert.AreEqual(resp, fromCache.HttpResponse);
        }


           [TestMethod]
        public async Task SetAndGetExisting()
        {
            var store = new ImmutableInMemoryCacheStore();

            var resp = new HttpResponseMessage();
            var cacheKey = new CacheKey("key", null);
            await store.SetAsync(cacheKey, new CacheEntry(resp));

            var respNew = new HttpResponseMessage();

            // overwrite
            await store.SetAsync(cacheKey, new CacheEntry(respNew));

            var fromCache = await store.GetAsync(cacheKey);

            // check
            Assert.AreEqual(respNew, fromCache.HttpResponse);
        }




        [TestMethod]
        public async Task SetAndGetMultiple()
        {
            var store = new ImmutableInMemoryCacheStore();

            var resp = new HttpResponseMessage();
            var resp2 = new HttpResponseMessage();

            var cacheKey = new CacheKey("key", null);
            var cacheKey2 = new CacheKey("key2", null);

            await store.SetAsync(cacheKey, new CacheEntry(resp));
            await store.SetAsync(cacheKey2, new CacheEntry(resp2));

            var fromCache = await store.GetAsync(cacheKey);
            var fromCache2 = await store.GetAsync(cacheKey2);

            // check
            Assert.AreEqual(resp, fromCache.HttpResponse);
            // check
            Assert.AreEqual(resp2, fromCache2.HttpResponse);
        }

           [TestMethod]
        public async Task GetNonExisting()
        {
            var store = new ImmutableInMemoryCacheStore();

            var resp = new HttpResponseMessage();
            var cacheKey = new CacheKey("key", null);

            await store.SetAsync(cacheKey, new CacheEntry(resp));

            var fromCache = await store.GetAsync("key2");

            // check
            Assert.AreEqual(default(CacheEntry), fromCache);
         

        }

           [TestMethod]
        public async Task GetNonExistingFromEmpty()
        {
            var store = new ImmutableInMemoryCacheStore();
            var cacheKey = new CacheKey("key", null);
            var fromCache = await store.GetAsync(cacheKey);

            // check
            Assert.AreEqual(default(CacheEntry), fromCache);
        }


           [TestMethod]
        public async Task SetAndClear()
        {
            var store = new ImmutableInMemoryCacheStore();

            var resp = new HttpResponseMessage();
            var cacheKey = new CacheKey("key", null);
            await store.SetAsync(cacheKey, new CacheEntry(resp));

            await store.ClearAsync();

            var fromCache = await store.GetAsync(cacheKey);

            // check
            Assert.AreEqual(default(CacheEntry), fromCache);
        }

    }
}
