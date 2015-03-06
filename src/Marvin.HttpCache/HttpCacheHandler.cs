using Marvin.HttpCache.Store;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Marvin.HttpCache
{
   public class HttpCacheHandler: DelegatingHandler
   {

       private readonly ICacheStore _cacheStore;

       private bool _enableConditionalPut = true;

       private bool _enableConditionalPatch = true;

       private bool _enableClearRelatedResourceRepresentationsAfterPut = true;

       private bool _enableClearRelatedResourceRepresentationsAfterPatch = true;

       private bool _forceRevalidationOfStaleResourceRepresentations = false;

       /// <summary>
       /// Instantiates the HttpCacheHandler
       /// </summary>
       public HttpCacheHandler()
           : this(new ImmutableInMemoryCacheStore(), new HttpCacheHandlerSettings())
		{
		}

       /// <summary>
       /// Instantiates the HttpCacheHandler
       /// </summary>
       /// <param name="cacheStore">An instance of an implementation of ICacheStore</param>
       public HttpCacheHandler(ICacheStore cacheStore)
           : this(cacheStore, new HttpCacheHandlerSettings())
       {
       }
        
       /// <summary>
       /// Instantiates the HttpCacheHandler
       /// </summary>
       /// <param name="cacheHandlerSettings">An instance of an implementation of IHttpCacheHandlerSettings</param>
       public HttpCacheHandler(IHttpCacheHandlerSettings cacheHandlerSettings)
           : this(new ImmutableInMemoryCacheStore(), cacheHandlerSettings)
       {
       }
       

       /// <summary>
       /// Instantiates the HttpCacheHandler
       /// </summary>
       /// <param name="cacheStore">An instance of an implementation of ICacheStore</param>
       /// <param name="cacheHandlerSettings">An instance of an implementation of IHttpCacheHandlerSettings</param>
       public HttpCacheHandler(ICacheStore cacheStore, IHttpCacheHandlerSettings cacheHandlerSettings)
       {
           if (cacheStore == null)
           {
               throw new ArgumentNullException("cacheStore", "Provided ICacheStore implementation cannot be null.");
           }

           if (cacheHandlerSettings == null)
           {
               throw new ArgumentNullException("cacheHandlerSettings", "Provided IHttpCacheHandlerSettings implementation cannot be null.");
           }

           _cacheStore = cacheStore;

           _forceRevalidationOfStaleResourceRepresentations = cacheHandlerSettings.ForceRevalidationOfStaleResourceRepresentations;
           _enableConditionalPatch = cacheHandlerSettings.EnableConditionalPatch;
           _enableConditionalPut = cacheHandlerSettings.EnableConditionalPut;
           _enableClearRelatedResourceRepresentationsAfterPatch = cacheHandlerSettings.EnableClearRelatedResourceRepresentationsAfterPatch;
           _enableClearRelatedResourceRepresentationsAfterPut = cacheHandlerSettings.EnableClearRelatedResourceRepresentationsAfterPut;

       }

      
       protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, 
           System.Threading.CancellationToken cancellationToken)
       {

           if (request.Method == HttpMethod.Put || request.Method.Method.ToLower() == "patch")
           {
               // PUT - PATCH
               return HandleHttpPutOrPatch(request, cancellationToken);  
           }
           else if (request.Method == HttpMethod.Get)
           {
               // GET
               return HandleHttpGet(request, cancellationToken);
           }       
           else
           {
               // DELETE - POST - OTHERS 
               return base.SendAsync(request, cancellationToken);
           }

       }

       private Task<HttpResponseMessage> HandleHttpPutOrPatch(HttpRequestMessage request,
           System.Threading.CancellationToken cancellationToken)
       {

           string primaryCacheKey = CacheKeyHelpers.CreatePrimaryCacheKey(request);
           var cacheKey = CacheKeyHelpers.CreateCacheKey(primaryCacheKey);
           
           // cached + conditional PUT or cached + conditional PATCH
           if ((_enableConditionalPut && request.Method == HttpMethod.Put)
               ||
               (_enableConditionalPatch && request.Method.Method.ToLower() == "patch"))
           {

               bool addCachingHeaders = false;
               HttpResponseMessage responseFromCache = null;

               // available in cache?
               var responseFromCacheAsTask = _cacheStore.GetAsync(cacheKey);
               if (responseFromCacheAsTask.Result != null)
               {
                   addCachingHeaders = true;
                   responseFromCache = responseFromCacheAsTask.Result.HttpResponse;
               }

               if (addCachingHeaders)
               {
                   // set etag / lastmodified.  Both are set for better compatibility
                   // with different backend caching systems.  
                   if (responseFromCache.Headers.ETag != null)
                   {
                       request.Headers.Add(HttpHeaderConstants.IfMatch,
                           responseFromCache.Headers.ETag.ToString());
                   }

                   if (responseFromCache.Content.Headers.LastModified != null)
                   {
                       request.Headers.Add(HttpHeaderConstants.IfUnmodifiedSince,
                           responseFromCache.Content.Headers.LastModified.Value.ToString("r"));
                   }
               }
           }
           
           return HandleSendAndContinuationForPutPatch(cacheKey, request, cancellationToken);
       }


       private Task<HttpResponseMessage> HandleHttpGet(HttpRequestMessage request, 
           System.Threading.CancellationToken cancellationToken)
       {
           // get VaryByHeaders - order in the request shouldn't matter, so order them so the
           // rest of the logic doesn't result in different keys.


           string primaryCacheKey = CacheKeyHelpers.CreatePrimaryCacheKey(request);// request.RequestUri.ToString();
           bool responseIsCached = false;
           HttpResponseMessage responseFromCache = null;
           IEnumerable<CacheEntry> cacheEntriesFromCache = null;

           // first, before even looking at the cache:
           // The Cache-Control: no-cache HTTP/1.1 header field is also intended for use in requests made by the client. 
           // It is a means for the browser to tell the server and any intermediate caches that it wants a 
           // fresh version of the resource. 

           if (request.Headers.CacheControl != null && request.Headers.CacheControl.NoCache)
           {
               // Don't get from cache.  Get from server.
               return HandleSendAndContinuation(
                   CacheKeyHelpers.CreateCacheKey(primaryCacheKey), request, cancellationToken, false); 
           }




           // available in cache?
           var cacheEntriesFromCacheAsTask = _cacheStore.GetAsync(primaryCacheKey);
           if (cacheEntriesFromCacheAsTask.Result != default(IEnumerable<CacheEntry>))
           {
               cacheEntriesFromCache = cacheEntriesFromCacheAsTask.Result;

               // TODO: for all of these, check the varyby headers (secondary key).  
               // An item is a match if secondary & primary keys both match!
               responseFromCache = cacheEntriesFromCache.First().HttpResponse;
               responseIsCached = true;
           }
            
           if (responseIsCached)
           {

               // set the accompanying request message
               responseFromCache.RequestMessage = request;

               // Check conditions that might require us to revalidate/check
              
               // we must assume "the worst": get from server.

               bool mustRevalidate = HttpResponseHelpers.MustRevalidate(responseFromCache); 

               if (mustRevalidate)
               {                  
				    // we must revalidate - add headers to the request for validation.  
                    //  
                    // we add both ETag & IfModifiedSince for better interop with various
                    // server-side caching handlers. 
                   //
                    if (responseFromCache.Headers.ETag != null)
				    {
					    request.Headers.Add(HttpHeaderConstants.IfNoneMatch,
                            responseFromCache.Headers.ETag.ToString());					
				    }
                
                    if (responseFromCache.Content.Headers.LastModified != null)
				    {
                        request.Headers.Add(HttpHeaderConstants.IfModifiedSince,
                            responseFromCache.Content.Headers.LastModified.Value.ToString("r"));
				    }

                    return HandleSendAndContinuation(
                        CacheKeyHelpers.CreateCacheKey(primaryCacheKey), request, cancellationToken, true);
               }
               else
               {
                   // response is allowed to be cached and there's
                   // no need to revalidate: return the cached response
                   return Task.FromResult(responseFromCache);  
               }
           }
           else
           {
               // response isn't cached.  Get it, and (possibly) add it to cache.
               return HandleSendAndContinuation(
                   CacheKeyHelpers.CreateCacheKey(primaryCacheKey), request, cancellationToken, false); 
           }


       }


       private Task<HttpResponseMessage> HandleSendAndContinuation(CacheKey cacheKey, HttpRequestMessage request,
         System.Threading.CancellationToken cancellationToken, bool mustRevalidate)
       {

           return base.SendAsync(request, cancellationToken)
                   .ContinueWith(
                    task =>
                    {

                        var serverResponse = task.Result;

                        // if we had to revalidate & got a 304 returned, that means
                        // we can get the response message from cache.
                        if (mustRevalidate && serverResponse.StatusCode == HttpStatusCode.NotModified)
                        {
                            var cacheEntry = _cacheStore.GetAsync(cacheKey).Result;
                            var responseFromCacheEntry = cacheEntry.HttpResponse;
                            responseFromCacheEntry.RequestMessage = request;

                            return responseFromCacheEntry;
                        }

                        if (serverResponse.IsSuccessStatusCode)
                        {

                            // ensure no NULL dates
                            if (serverResponse.Headers.Date == null)
                            {
                                serverResponse.Headers.Date = DateTimeOffset.UtcNow;
                            }

                            // check the response: is this response allowed to be cached?
                            bool isCacheable = HttpResponseHelpers.CanBeCached(serverResponse);

                            if (isCacheable)
                            {
                                // add the response to cache
                                _cacheStore.SetAsync(cacheKey, new CacheEntry(serverResponse));
                            }


                            // what about vary by headers (=> key should take this into account)?
                            
                        }

                        return serverResponse;
                    });
       }


 

       private Task<HttpResponseMessage> HandleSendAndContinuationForPutPatch(CacheKey cacheKey, HttpRequestMessage request,
           System.Threading.CancellationToken cancellationToken)
       {

           return base.SendAsync(request, cancellationToken)
                   .ContinueWith(
                    task =>
                    {

                        var serverResponse = task.Result;

                        if (serverResponse.IsSuccessStatusCode)
                        {

                            // ensure no NULL dates
                            if (serverResponse.Headers.Date == null)
                            {
                                serverResponse.Headers.Date = DateTimeOffset.UtcNow;
                            }

                            // should we clear?

                            if ((_enableClearRelatedResourceRepresentationsAfterPut && request.Method == HttpMethod.Put)
                                ||
                                (_enableClearRelatedResourceRepresentationsAfterPatch && request.Method.Method.ToLower() == "patch"))
                            {
                                // clear related resources 
                                // 
                                // - remove resource with cachekey.  This must be done, as there's no 
                                // guarantee the new response is cacheable.
                                //
                                // - look for resources in cache that start with 
                                // the cachekey + "?" for querystring.

                                _cacheStore.RemoveAsync(cacheKey);
                                _cacheStore.RemoveRangeAsync(cacheKey.PrimaryKey + "?");
                            } 

                            
                            // check the response: is this response allowed to be cached?
                            bool isCacheable = HttpResponseHelpers.CanBeCached(serverResponse);

                            if (isCacheable)
                            {
                                // add the response to cache
                                _cacheStore.SetAsync(cacheKey, new CacheEntry(serverResponse));
                            }
                            
                            // what about vary by headers (=> key should take this into account)?

                        }

                        return serverResponse;

                    });
       }

   }
}