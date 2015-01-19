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

            await store.SetAsync("key", resp);
            
            var fromCache = await store.GetAsync("key");

            // check
            Assert.AreEqual(resp, fromCache);
        }

           [TestMethod]
        public async Task SetAndGetExisting()
        {
            var store = new ImmutableInMemoryCacheStore();

            var resp = new HttpResponseMessage();

            await store.SetAsync("key", resp);

            var respNew = new HttpResponseMessage();

            // overwrite
            await store.SetAsync("key", respNew);

            var fromCache = await store.GetAsync("key");

            // check
            Assert.AreEqual(respNew, fromCache);
        }



        [TestMethod]
        public async Task SetAndGetMultiple()
        {
            var store = new ImmutableInMemoryCacheStore();

            var resp = new HttpResponseMessage();
            var resp2 = new HttpResponseMessage();

            await store.SetAsync("key", resp);
            await store.SetAsync("key2", resp2);

            var fromCache = await store.GetAsync("key");
            var fromCache2 = await store.GetAsync("key2");

            // check
            Assert.AreEqual(resp, fromCache);
            // check
            Assert.AreEqual(resp2, fromCache2);
        }

           [TestMethod]
        public async Task GetNonExisting()
        {
            var store = new ImmutableInMemoryCacheStore();

            var resp = new HttpResponseMessage();
          
            await store.SetAsync("key", resp);

            var fromCache = await store.GetAsync("key2");

            // check
            Assert.AreEqual(default(HttpResponseMessage), fromCache);
         

        }

           [TestMethod]
        public async Task GetNonExistingFromEmpty()
        {
            var store = new ImmutableInMemoryCacheStore();
                    
            var fromCache = await store.GetAsync("key");

            // check
            Assert.AreEqual(default(HttpResponseMessage), fromCache);
        }


           [TestMethod]
        public async Task SetAndClear()
        {
            var store = new ImmutableInMemoryCacheStore();

            var resp = new HttpResponseMessage();

            await store.SetAsync("key", resp);

            await store.ClearAsync();

            var fromCache = await store.GetAsync("key");

            // check
            Assert.AreEqual(default(HttpResponseMessage), fromCache);
        }

    }
}
