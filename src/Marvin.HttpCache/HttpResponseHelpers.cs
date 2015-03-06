using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Marvin.HttpCache
{
    internal static class HttpResponseHelpers
    {
        // 13.4: A response received with a status code of 200, 203, 206, 300, 301 or 410 may be stored 
       static List<HttpStatusCode> _httpStatusCodesThatAllowCaching = new List<HttpStatusCode>()
		    {
				HttpStatusCode.OK, // 200
                HttpStatusCode.NonAuthoritativeInformation, // 203
       			HttpStatusCode.PartialContent, // 206
                HttpStatusCode.MultipleChoices, // 300
				HttpStatusCode.MovedPermanently, // 301
                HttpStatusCode.Gone // 410
			};

        internal static bool CanBeCached(HttpResponseMessage serverResponse)
        {
            
            if (!(_httpStatusCodesThatAllowCaching.Contains(serverResponse.StatusCode)))
            {
                return false;
            }

            // vary by header * => not cachable
            if (serverResponse.Headers.Vary.Contains("*"))
            {
                return false;
            }

            // no content or cache header => not cacheable
            if (serverResponse.Content == null || serverResponse.Headers.CacheControl == null)
            {
                return false;
            }

            // no store => client must not cache
            if (serverResponse.Headers.CacheControl.NoStore)
            {
                return false;
            }

            // no expires nor maxage nor sharedmaxage are defined => no cache
            if (serverResponse.Content.Headers.Expires == null 
                && serverResponse.Headers.CacheControl.MaxAge == null 
                && serverResponse.Headers.CacheControl.SharedMaxAge == null)
            {
                return false;
            }
            
            return true;

        }



        internal static bool MustRevalidate(HttpResponseMessage responseFromCache)
        {
            // should we revalidate?
            if (responseFromCache.Content == null || responseFromCache.Headers.CacheControl == null)
            {
                // something went wrong - revalidate
                return true;
            }

            // if nocache is defined, this means we must revalidate on each request.
            if (responseFromCache.Headers.CacheControl.NoCache)
            {
                return true;
            }

            // shared age overrides max age, and that in turn overrides
            // expires, EVEN if lower-importance values are more forgiving! 

            // first, check shared age if available.  
            // next, check max age if available.
            // lastly, check expires

            if  (responseFromCache.Headers.CacheControl.SharedMaxAge != null)
            {
                // CANNOT combine this in one, as higher-importance values override
                // lower-importance values.  If there's a sharedmaxage, that's the
                // only one taken into account - even if the date doesn't result in
                // a revalidate
                if  (responseFromCache.Headers.Date.Value.Add(responseFromCache.Headers.CacheControl.SharedMaxAge.Value)
                <= DateTime.UtcNow)
                {
                    // This means the response is stale.  A client can keep
                    // on working with stale responses, unless must-revalidate is defined. 
                    
                    // TO BE IMPLEMENTED: AlwaysRevalidateStaleResponses option: this
                    // should ensure revalidation is always done, regardless of 
                    // must-revalidate
                    return responseFromCache.Headers.CacheControl.MustRevalidate;
                }
            }
            else if (responseFromCache.Headers.CacheControl.MaxAge != null)
            {
                if (responseFromCache.Headers.Date.Value.Add(responseFromCache.Headers.CacheControl.MaxAge.Value)
                <= DateTime.UtcNow)
                {
                    return responseFromCache.Headers.CacheControl.MustRevalidate;
                }
            }
            else if (responseFromCache.Content.Headers.Expires != null
                && responseFromCache.Content.Headers.Expires < DateTimeOffset.UtcNow)
            {
                return responseFromCache.Headers.CacheControl.MustRevalidate;
            } 

            return false;

        }
    }
}
