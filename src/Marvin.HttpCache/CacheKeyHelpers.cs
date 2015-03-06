using Marvin.HttpCache.Store;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Marvin.HttpCache
{
    internal static class CacheKeyHelpers
    {

        internal static string CreatePrimaryCacheKey(HttpRequestMessage request)
        {
            return request.RequestUri.ToString().ToLower();
        }


        internal static CacheKey CreateCacheKey(string primaryCacheKey, string secondaryCacheKey)
        {
            return new CacheKey(primaryCacheKey, secondaryCacheKey);
        }


        internal static CacheKey CreateCacheKey(string primaryCacheKey)
        {
            return new CacheKey(primaryCacheKey, null);
        }
    }
}
