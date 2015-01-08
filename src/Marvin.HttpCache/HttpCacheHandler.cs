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
           string uri = request.RequestUri.ToString();

           // are we cached?
           var responseFromCache = _cacheStore.GetAsync(uri);
           if (responseFromCache.Result != null)
               return responseFromCache;

           // we're not cached; SendAsync, afterwards add to cache store
           return base.SendAsync(request, cancellationToken)
               .ContinueWith(
                task =>
                {
                    var serverResponse = task.Result;
                    
                    // add the response to cache
                    _cacheStore.SetAsync(uri, serverResponse);

                    return serverResponse;
                });

       }
   }
}