using Marvin.HttpCache.Store;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Marvin.HttpCache
{
   public class HttpCacheHandler: DelegatingHandler
   {

       private readonly ICacheStore<string, HttpResponseMessage> _cacheStore;



       public HttpCacheHandler()
           : this(new ImmutableInMemoryCacheStore<string, HttpResponseMessage>())
		{
		}


       public HttpCacheHandler(ICacheStore<string, HttpResponseMessage> cacheStore)
       {
           _cacheStore = cacheStore;
       }

      
       protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, 
           System.Threading.CancellationToken cancellationToken)
       {

           // only GET implemented currently

           if (request.Method != HttpMethod.Get)
           {
               return base.SendAsync(request, cancellationToken);
           }
           else
           {
               return HandleHttpGet(request, cancellationToken);
           }

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

               bool mustRevalidate = MustRevalidate(responseFromCache); 

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
 
                    return HandleSendAndContinuation(cacheKey, request, cancellationToken);
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
               return HandleSendAndContinuation(cacheKey, request, cancellationToken); 
           }


       }

       private bool MustRevalidate(HttpResponseMessage responseFromCache)
       {
           // should we revalidate?
           if (responseFromCache.Content == null)
           {
               // something went wrong - revalidate
               return true;
           }               

           // has the expired date passed? 
           if ((responseFromCache.Content.Headers.Expires != null &&
               responseFromCache.Content.Headers.Expires < DateTimeOffset.UtcNow)
               || 
               // OR is maxage passed?
               (responseFromCache.Headers.CacheControl != null
               && responseFromCache.Headers.CacheControl.MaxAge != null 
               && (responseFromCache.Headers.Date.Value.Add(responseFromCache.Headers.CacheControl.MaxAge.Value)
               <= DateTime.UtcNow))
               ||
               // OR is sharedmaxage passed?
               (responseFromCache.Headers.CacheControl != null
               && responseFromCache.Headers.CacheControl.SharedMaxAge != null 
               && (responseFromCache.Headers.Date.Value.Add(responseFromCache.Headers.CacheControl.SharedMaxAge.Value)
               <= DateTime.UtcNow))
               )

           {
               // This means the response is stale.  A client can keep
               // on working with stale responses, unless must-revalidate is defined

               var cacheControlHeader = responseFromCache.Headers.CacheControl;
               if (cacheControlHeader == null)
               {
                   return false;
               }
               else
               {
                   return cacheControlHeader.MustRevalidate;
               }

               // TO BE IMPLEMENTED: AlwaysRevalidateStaleResponses option: this
               // should ensure revalidation is always done, regardless of 
               // must-revalidate
           }

           return false;

       }

       private Task<HttpResponseMessage> HandleSendAndContinuation(string cacheKey, HttpRequestMessage request, 
           System.Threading.CancellationToken cancellationToken)
       {
           
           return base.SendAsync(request, cancellationToken)
                   .ContinueWith(
                    task =>
                    {
                        bool isCacheable = false;

                        var serverResponse = task.Result;

                        // ensure no NULL dates
                        if (serverResponse.Headers.Date == null)
                        {
                            serverResponse.Headers.Date = DateTimeOffset.UtcNow;
                        }
                    
                        // check the response: is this response allowed to be cached?

                        // what about vary by headers?


                        // add the response to cache
                        _cacheStore.SetAsync(cacheKey, serverResponse);
                        return serverResponse;

                    });
       }

 


   }
}