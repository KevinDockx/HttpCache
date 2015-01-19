﻿using Marvin.HttpCache.Store;
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

       private bool _enableRelatedResourceClearAfterPut = true;

       private bool _enableRelatedResourceClearAfterPatch = true;
        
       

       /// <summary>
       /// Instantiates the HttpCacheHandler
       /// </summary>
       /// <param name="enableConditionalPut">When conditional put is enabled, 
       /// IfMatch and IfUnmodifiedSince headers are added tot the request so the update is only
       /// executed if the cached version on the client matches the version on the server.  This is
       /// the default.
       ///
       /// If conditional put isn't enabled, we send the put request through no matter what - 
       /// this means that PUT is executed even if the version on the client doesn't match
       /// that on the server.  </param>
       public HttpCacheHandler(bool enableConditionalPut = true)
           : this(new ImmutableInMemoryCacheStore(), enableConditionalPut)
		{
		}

       /// <summary>
       /// Instantiates the HttpCacheHandler
       /// </summary>
       /// <param name="cacheStore">An instance of an ICacheStore</param>
       /// <param name="enableConditionalPut">When conditional put is enabled, 
       /// IfMatch and IfUnmodifiedSince headers are added tot the request so the update is only
       /// executed if the cached version on the client matches the version on the server.  This is
       /// the default.
       ///
       /// If conditional put isn't enabled, we send the put request through no matter what - 
       /// this means that PUT is executed even if the version on the client doesn't match
       /// that on the server.  </param>
       public HttpCacheHandler(ICacheStore cacheStore, bool enableConditionalPut = true)
       {
           _cacheStore = cacheStore;
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

           string cacheKey = request.RequestUri.ToString();
           
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
                   responseFromCache = responseFromCacheAsTask.Result;
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
           string cacheKey = request.RequestUri.ToString();
           bool responseIsCached = false;
           HttpResponseMessage responseFromCache = null;

           // available in cache?
           var responseFromCacheAsTask = _cacheStore.GetAsync(cacheKey);
           if (responseFromCacheAsTask.Result != null)
           {
               responseIsCached = true;
               responseFromCache = responseFromCacheAsTask.Result;
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
 
                    return HandleSendAndContinuation(cacheKey, request, cancellationToken, true);
               }
               else
               {
                   // response is allowed to be cached and there's
                   // no need to revalidate: return the cached response
                   return responseFromCacheAsTask;
               }
           }
           else
           {
               // response isn't cached.  Get it, and (possibly) add it to cache.
               return HandleSendAndContinuation(cacheKey, request, cancellationToken, false); 
           }


       }


       private Task<HttpResponseMessage> HandleSendAndContinuation(string cacheKey, HttpRequestMessage request, 
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
                            // get from cache
                            var resp = _cacheStore.GetAsync(cacheKey).Result;
                            // set request
                            resp.RequestMessage = request;
                            // return response
                            return resp;
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
    
                                // max-age overrides expires header, and 
                                // shared max age overrides both.  Only keep 
                                // one of these values.
                                //
                                // note: changed.  Keep all values - no tinkering with the
                                // response, values are checked for revalidate.  
                                //
                                //if (serverResponse.Headers.CacheControl != null 
                                //    && 
                                //        (serverResponse.Headers.CacheControl.MaxAge != null || 
                                //         serverResponse.Headers.CacheControl.SharedMaxAge != null)
                                //    &&
                                //    serverResponse.Content.Headers.Expires != null)
                                //{
                                //    serverResponse.Content.Headers.Expires = null;
                                //}


                                // add the response to cache
                                _cacheStore.SetAsync(cacheKey, serverResponse);
                            }
                            

                            // what about vary by headers (=> key should take this into account)?


                        }

                        return serverResponse;

                    });
       }



       private Task<HttpResponseMessage> HandleSendAndContinuationForPutPatch(string cacheKey, HttpRequestMessage request,
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

                            if ((_enableRelatedResourceClearAfterPut && request.Method == HttpMethod.Put)
                                ||
                                (_enableRelatedResourceClearAfterPatch && request.Method.Method.ToLower() == "patch"))
                            {
                                // clear related resources 
                                // 
                                // - remove resource with cachekey.  This must be done, as there's no 
                                // guarantee the new response is cacheable.
                                //
                                // - look for resources in cache that start with 
                                // the cachekey + "?" for querystring.

                                _cacheStore.RemoveAsync(cacheKey);
                                _cacheStore.RemoveRangeAsync(cacheKey + "?");
                            } 

                            
                            // check the response: is this response allowed to be cached?
                            bool isCacheable = HttpResponseHelpers.CanBeCached(serverResponse);

                            if (isCacheable)
                            {
                                // add the response to cache
                                _cacheStore.SetAsync(cacheKey, serverResponse);
                            }
                            
                            // what about vary by headers (=> key should take this into account)?

                        }

                        return serverResponse;

                    });
       }

   }
}