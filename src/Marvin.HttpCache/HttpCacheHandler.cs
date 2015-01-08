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


       private Task<HttpResponseMessage> HandleSendAndContinuation(string cacheKey, HttpRequestMessage request, 
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
                            
                            // check the response: is this response allowed to be cached?
                            bool isCacheable = HttpResponseHelpers.CanBeCached(serverResponse);

                            if (isCacheable)
                            {
    
                                // max-age overrides expires header, and 
                                // shared max age overrides both.  Only keep 
                                // one of these value.
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



   }
}