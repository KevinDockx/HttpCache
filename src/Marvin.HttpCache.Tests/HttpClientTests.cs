using Marvin.HttpCache.Store;
using Marvin.HttpCache.Tests.Mock;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Marvin.HttpCache.Tests
{ 



    [TestClass]
    public class HttpClientTests
    {

        private const string _testUri = "http://www.myapi.com/testresources";
        private const string _eTag = "\"dummyetag\"";
        private ImmutableInMemoryCacheStore _store;
        private MockHttpMessageHandler _mockHandler;


        private HttpClient InitClient()
        {
     
            _store = new ImmutableInMemoryCacheStore();
            _mockHandler = new MockHttpMessageHandler();
            var httpClient = new HttpClient(
                new HttpCacheHandler(_store)
                  {
                      InnerHandler = _mockHandler
                  });

            return httpClient;
        }







        private HttpResponseMessage GetResponseMessage(bool mustRevalidate)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Headers.Date = DateTimeOffset.UtcNow;
            response.Content = new ByteArrayContent(new byte[512]);
            response.Headers.CacheControl = new CacheControlHeaderValue()
            {
                MustRevalidate = mustRevalidate,
                Public = true,
                MaxAge = TimeSpan.FromSeconds(666),

            };

            return response;
        }

        [TestMethod]
        public void GetShouldInsertInCacheStore()
        {
            var req = new HttpRequestMessage(HttpMethod.Get, _testUri);
            var resp = GetResponseMessage(false);
            var httpClient = InitClient();

            _mockHandler.Response = resp;

            var result = httpClient.SendAsync(req).Result;

            // result should be in cache
            var fromCache = _store.GetAsync(new CacheKey(_testUri)).Result;

            Assert.AreEqual(result, fromCache.HttpResponse);

            // result should be the same as response
            Assert.AreEqual(resp, result);

        }

        [TestMethod]
        public void GetFromCacheStoreNoRevalidate()
        {
            // first GET: insert in cache
            var req = new HttpRequestMessage(HttpMethod.Get, _testUri);
            var resp = GetResponseMessage(false);
            var httpClient = InitClient();

            _mockHandler.Response = resp;

            var result = httpClient.SendAsync(req).Result;

            // second GET: get from cache
            var req2 = new HttpRequestMessage(HttpMethod.Get, _testUri);
            var result2 = httpClient.SendAsync(req2).Result;

            // get from cache. Must match response, result and second result
            var fromCache = _store.GetAsync(new CacheKey(_testUri)).Result;

            Assert.AreEqual(result, fromCache.HttpResponse);
            Assert.AreEqual(result, resp);
            Assert.AreEqual(result2, result);

        }

        [TestMethod]
        public void GetStaleFromCacheStoreNoRevalidate()
        {

            // first GET: insert in cache
            var req = new HttpRequestMessage(HttpMethod.Get, _testUri);
            var resp = GetResponseMessage(false);

            // ensure stale:
            resp.Headers.CacheControl.SharedMaxAge = new TimeSpan(-100); 

            var httpClient = InitClient();

            _mockHandler.Response = resp;

            var result = httpClient.SendAsync(req).Result;

            // second GET: get from cache
            var req2 = new HttpRequestMessage(HttpMethod.Get, _testUri);
            var result2 = httpClient.SendAsync(req2).Result;

            // get from cache. Must match response, result and second result
            var fromCache = _store.GetAsync(new CacheKey(_testUri)).Result;

            Assert.AreEqual(result, fromCache.HttpResponse);
            Assert.AreEqual(result, resp);
            Assert.AreEqual(result2, result);


        }

        [TestMethod]
        public void MustRevalidateDueToNoCache()
        {
            // first GET: insert in cache
            var req = new HttpRequestMessage(HttpMethod.Get, _testUri);
            var resp = GetResponseMessage(false);

            // will revalidate even with "false" as mustrevalidate value
            resp.Headers.CacheControl.NoCache = true;
            resp.Headers.ETag = new EntityTagHeaderValue(_eTag);
            var httpClient = InitClient();

            _mockHandler.Response = resp;

            var result = httpClient.SendAsync(req).Result;

            // second GET: should revalidate and return 304 
            // which should then mean it returns the item from cache
            var req2 = new HttpRequestMessage(HttpMethod.Get, _testUri);
            var respNotModified = new HttpResponseMessage(HttpStatusCode.NotModified);
            _mockHandler.Response = respNotModified;

            var result2 = httpClient.SendAsync(req2).Result;

            // get from cache. Must match response, result and second result
            var fromCache = _store.GetAsync(new CacheKey(_testUri)).Result;

            Assert.AreEqual(result, fromCache.HttpResponse);
            Assert.AreEqual(result, resp);
            Assert.AreEqual(result2, result);
            
            // request must now have a matching IfNoneMatch header
            Assert.AreEqual(_eTag, req2.Headers.IfNoneMatch.First().Tag);
        
        }

        [TestMethod]
        public void MustRevalidateDueToSharedMaxAge()
        {
            // first GET: insert in cache
            var req = new HttpRequestMessage(HttpMethod.Get, _testUri);
            var resp = GetResponseMessage(true);
                   
            resp.Headers.CacheControl.SharedMaxAge = new TimeSpan(-100);
            resp.Headers.ETag = new EntityTagHeaderValue(_eTag);
            var httpClient = InitClient();

            _mockHandler.Response = resp;

            var result = httpClient.SendAsync(req).Result;

            // second GET: should revalidate and return 304 
            // which should then mean it returns the item from cache
            var req2 = new HttpRequestMessage(HttpMethod.Get, _testUri);
            var respNotModified = new HttpResponseMessage(HttpStatusCode.NotModified);
            _mockHandler.Response = respNotModified;

            var result2 = httpClient.SendAsync(req2).Result;

            // get from cache. Must match response, result and second result
            var fromCache = _store.GetAsync(new CacheKey(_testUri)).Result;

            Assert.AreEqual(result, fromCache.HttpResponse);
            Assert.AreEqual(result, resp);
            Assert.AreEqual(result2, result);

            // request must now have a matching IfNoneMatch header
            Assert.AreEqual(_eTag, req2.Headers.IfNoneMatch.First().Tag);
        }


        [TestMethod]
        public void MustNotRevalidateEvenWithSharedMaxAge()
        {
            // first GET: insert in cache
            var req = new HttpRequestMessage(HttpMethod.Get, _testUri);
            // no revalidation
            var resp = GetResponseMessage(false);

            resp.Headers.CacheControl.SharedMaxAge = new TimeSpan(-100);
            resp.Headers.ETag = new EntityTagHeaderValue(_eTag);
            var httpClient = InitClient();

            _mockHandler.Response = resp;

            var result = httpClient.SendAsync(req).Result;

            // second GET: should not revalidate, just get from cache 
            var req2 = new HttpRequestMessage(HttpMethod.Get, _testUri);           
            var result2 = httpClient.SendAsync(req2).Result;

            // get from cache. Must match response, result and second result
            var fromCache = _store.GetAsync(new CacheKey(_testUri)).Result;

            Assert.AreEqual(result, fromCache.HttpResponse);
            Assert.AreEqual(result, resp);
            Assert.AreEqual(result2, result);

            // request must still have null IfNoneMatch
            Assert.AreEqual(null, req2.Headers.IfNoneMatch.FirstOrDefault());
        }



        [TestMethod]
        public void MustRevalidateDueToMaxAge()
        { 
            // first GET: insert in cache
            var req = new HttpRequestMessage(HttpMethod.Get, _testUri);
            var resp = GetResponseMessage(true);

            resp.Headers.CacheControl.MaxAge = new TimeSpan(-100);
            resp.Headers.ETag = new EntityTagHeaderValue(_eTag);
            var httpClient = InitClient();

            _mockHandler.Response = resp;

            var result = httpClient.SendAsync(req).Result;

            // second GET: should revalidate and return 304 
            // which should then mean it returns the item from cache
            var req2 = new HttpRequestMessage(HttpMethod.Get, _testUri);
            var respNotModified = new HttpResponseMessage(HttpStatusCode.NotModified);
            _mockHandler.Response = respNotModified;

            var result2 = httpClient.SendAsync(req2).Result;

            // get from cache. Must match response, result and second result
            var fromCache = _store.GetAsync(new CacheKey(_testUri)).Result;

            Assert.AreEqual(result, fromCache.HttpResponse);
            Assert.AreEqual(result, resp);
            Assert.AreEqual(result2, result);

            // request must now have a matching IfNoneMatch header
            Assert.AreEqual(_eTag, req2.Headers.IfNoneMatch.First().Tag);
        }


        [TestMethod]
        public void MustNotRevalidateEvenWithToMaxAge()
        {    
            // first GET: insert in cache
            var req = new HttpRequestMessage(HttpMethod.Get, _testUri);
            // no revalidation
            var resp = GetResponseMessage(false);

            resp.Headers.CacheControl.MaxAge = new TimeSpan(-100);
            resp.Headers.ETag = new EntityTagHeaderValue(_eTag);
            var httpClient = InitClient();

            _mockHandler.Response = resp;

            var result = httpClient.SendAsync(req).Result;

            // second GET: should not revalidate, just get from cache 
            var req2 = new HttpRequestMessage(HttpMethod.Get, _testUri);
            var result2 = httpClient.SendAsync(req2).Result;

            // get from cache. Must match response, result and second result
            var fromCache = _store.GetAsync(new CacheKey(_testUri)).Result;

            Assert.AreEqual(result, fromCache.HttpResponse);
            Assert.AreEqual(result, resp);
            Assert.AreEqual(result2, result);

            // request must still have null IfNoneMatch
            Assert.AreEqual(null, req2.Headers.IfNoneMatch.FirstOrDefault());
        }



        [TestMethod]
        public void MustRevalidateDueToExpired()
        { 
            // first GET: insert in cache
            var req = new HttpRequestMessage(HttpMethod.Get, _testUri);
            var resp = GetResponseMessage(true);

            resp.Headers.CacheControl.SharedMaxAge = null;
            resp.Headers.CacheControl.MaxAge = null;
            resp.Content.Headers.Expires = new DateTimeOffset(new DateTime(1, 1, 2));
            resp.Headers.ETag = new EntityTagHeaderValue(_eTag);

            var httpClient = InitClient();

            _mockHandler.Response = resp;

            var result = httpClient.SendAsync(req).Result;

            // second GET: should revalidate and return 304 
            // which should then mean it returns the item from cache
            var req2 = new HttpRequestMessage(HttpMethod.Get, _testUri);
            var respNotModified = new HttpResponseMessage(HttpStatusCode.NotModified);
            _mockHandler.Response = respNotModified;

            var result2 = httpClient.SendAsync(req2).Result;

            // get from cache. Must match response, result and second result
            var fromCache = _store.GetAsync(new CacheKey(_testUri)).Result;

            Assert.AreEqual(result, fromCache.HttpResponse);
            Assert.AreEqual(result, resp);
            Assert.AreEqual(result2, result);

            // request must now have a matching IfNoneMatch header
            Assert.AreEqual(_eTag, req2.Headers.IfNoneMatch.First().Tag);
        }


        [TestMethod]
        public void MustNotRevalidateEvenWithExpired()
        {
            // first GET: insert in cache
            var req = new HttpRequestMessage(HttpMethod.Get, _testUri);
            // no revalidation
            var resp = GetResponseMessage(false);

            resp.Headers.CacheControl.SharedMaxAge = null;
            resp.Headers.CacheControl.MaxAge = null;
            resp.Content.Headers.Expires = new DateTimeOffset(new DateTime(1, 1, 2));
            resp.Headers.ETag = new EntityTagHeaderValue(_eTag);
            var httpClient = InitClient();

            _mockHandler.Response = resp;

            var result = httpClient.SendAsync(req).Result;

            // second GET: should not revalidate, just get from cache 
            var req2 = new HttpRequestMessage(HttpMethod.Get, _testUri);
            var result2 = httpClient.SendAsync(req2).Result;

            // get from cache. Must match response, result and second result
            var fromCache = _store.GetAsync(new CacheKey(_testUri)).Result;

            Assert.AreEqual(result, fromCache.HttpResponse);
            Assert.AreEqual(result, resp);
            Assert.AreEqual(result2, result);

            // request must still have null IfNoneMatch
            Assert.AreEqual(null, req2.Headers.IfNoneMatch.FirstOrDefault());
        }
 
        [TestMethod]
        public void NoCacheDueToNoStore()
        {

            var req = new HttpRequestMessage(HttpMethod.Get, _testUri);
            var resp = GetResponseMessage(false);
            var httpClient = InitClient();

            // no store
            resp.Headers.CacheControl.NoStore = true;
            _mockHandler.Response = resp;

            var result = httpClient.SendAsync(req).Result;

            // result should NOT be in cache
            var fromCache = _store.GetAsync(_testUri).Result;
            Assert.AreEqual(null, fromCache);

        }

        [TestMethod]
        public void NoCacheDueToNoExpiresMaxAgeSharedMaxAge()
        {
            var req = new HttpRequestMessage(HttpMethod.Get, _testUri);
            var resp = GetResponseMessage(false);
            var httpClient = InitClient();
 
            resp.Headers.CacheControl.MaxAge = null;
            resp.Headers.CacheControl.SharedMaxAge = null;
            resp.Content.Headers.Expires = null;
            _mockHandler.Response = resp;

            var result = httpClient.SendAsync(req).Result;

            // result should NOT be in cache
            var fromCache = _store.GetAsync(_testUri).Result;
            Assert.AreEqual(null, fromCache);
        }
    }
}
