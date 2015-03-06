using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Marvin.HttpCache.Store
{

    /// <summary>
    /// The cache key - this can vary by:
    /// - method
    /// - URI
    /// - VaryByHeaders
    /// </summary>
    public class CacheKey
    {

        public string PrimaryKey { get; private set; }

        public string SecondaryKey { get; private set; }

        //public string Uri { get; private set; }

        //public HttpMethod Method { get; private set; }


        
 

        public string UnifiedKey { get; private set; }


        public CacheKey(string primaryCacheKey)
        {
            // create a new cachekey from the response.  Do check the VaryByHeaders => dictionary?  
            // Should list all the headers defined in the vary by headers value.  
            //
            // Need fast lookup - create string key from all this?

            PrimaryKey = primaryCacheKey ?? "";
            SecondaryKey = "";
        }

    
        public CacheKey(string primaryCacheKey, string secondaryCacheKey) : this(primaryCacheKey)
        {
            // create a new cachekey from the response.  Do check the VaryByHeaders => dictionary?  
            // Should list all the headers defined in the vary by headers value.  
            //
            // Need fast lookup - create string key from all this?
 
            SecondaryKey = secondaryCacheKey ?? "";


        }

        public override bool Equals(object obj)
        {
            var secondCacheKey = (CacheKey)obj;
            return secondCacheKey.PrimaryKey == PrimaryKey
                && secondCacheKey.SecondaryKey == SecondaryKey;
        }

        public override int GetHashCode()
        {
            int hash = 13;
            hash = (hash * 7) + PrimaryKey.GetHashCode();
            hash = (hash * 7) + SecondaryKey.GetHashCode(); 
            return hash; 
        }

    }
}
