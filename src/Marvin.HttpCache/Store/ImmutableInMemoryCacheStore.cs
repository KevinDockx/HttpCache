using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Marvin.HttpCache.Store
{

    public class ImmutableInMemoryCacheStore : ICacheStore
    { 

        private IImmutableDictionary<CacheKey, CacheEntry> _cache = ImmutableDictionary.Create<CacheKey,
            CacheEntry>(); 
    

        public Task<IEnumerable<CacheEntry>> GetAsync(string primaryKey)
        {
            primaryKey = primaryKey.ToLower();

            var validCacheKeys = _cache.Keys.Where(k => k.PrimaryKey == primaryKey);

            if (validCacheKeys.Any())
            {                
                var selectedValues = validCacheKeys.Where(_cache.ContainsKey)
                         .Select(k => _cache[k]);
                     
                return Task.FromResult(selectedValues);
            }
            else
            {
                return Task.FromResult(default(IEnumerable<CacheEntry>));
            }

        }



        public Task<CacheEntry> GetAsync(CacheKey key)
        {
            CacheEntry value;
            if (_cache.TryGetValue(key, out value))
            {
                return Task.FromResult((CacheEntry)value);
            }
            else
            {
                return Task.FromResult(default(CacheEntry));
            }
        }
 

        public Task SetAsync(CacheKey key, CacheEntry value)
        {
            do
            {
             
                var oldCache = _cache;
                IImmutableDictionary<CacheKey, CacheEntry> newCache;

                if (oldCache.ContainsKey(key))
                {
                    // overwrite.  Dic is immutable: no lock needed.
                    newCache = oldCache.SetItem(key, value);
                }
                else
                {
                    // Add the value to cache dictionary.  Dic is immutable: no lock needed.
                    newCache = oldCache.Add(key, value);
                }

                // newCache = new cache dic, containing value.  Check.

                // Interlocked.CompareExchange(ref _cache, newCache, oldCache): 
                //
                // => if _cache is the same as oldcache, then  replace
                // _cache by newCache.  This is an effective check: if _cache is no longer the
                // same as oldCache, another thread has made a change to _cache.  If that's the
                // case, we need to do the add again, as we'll want to make sure we always work 
                // on the latest version - we don't want to loose changes to the cache.
                //
                // Call checks for reference equality, not an overridden Equals => we need
                // this reference check, new instance = different reference.
                //
                // CompareExchange always returns the value in "location", eg the first 
                // parameter, BEFORE the exchange.  So, if we check that value (_cache before 
                // exchange) against the oldCache and if these are the same, add was succesful,
                // and thanks to the CompareExchange call, _cache is now set to newCache

                // compares oldCache with newCache - if these are now the s
                if (oldCache == Interlocked.CompareExchange(ref _cache, newCache, oldCache))
                {
                    // we can get out of the loop

                    return Task.FromResult(true);
                }

                // CompareExchange failed => another thread has made a change to _cache.
                // We need to do the add again, as we'll want to make sure we always work 
                // on the latest version - we don't want to loose changes to the cache.

            } while (true);
        }

        public Task RemoveAsync(CacheKey key)
        {
            do
            {
            

                var oldCache = _cache;
                IImmutableDictionary<CacheKey, CacheEntry> newCache;

                if (oldCache.ContainsKey(key))
                {
                    // Remove.  Dic is immutable: no lock needed.
                    newCache = oldCache.Remove(key);
                }
                else
                {
                    newCache = oldCache;
                }

                // compares oldCache with newCache - if these are now the s
                if (oldCache == Interlocked.CompareExchange(ref _cache, newCache, oldCache))
                {
                    // we can get out of the loop

                    return Task.FromResult(true);
                }

                // CompareExchange failed => another thread has made a change to _cache.
                // We need to do the add again, as we'll want to make sure we always work 
                // on the latest version - we don't want to loose changes to the cache.

            } while (true);

        }
         


        public Task RemoveRangeAsync(string primaryKeyStartsWith)
        {

            do
            {
                primaryKeyStartsWith = primaryKeyStartsWith.ToLower();

                var oldCache = _cache;
                IImmutableDictionary<CacheKey, CacheEntry> newCache;
                
                var listOfKeys = oldCache.Keys.Where(k => k.PrimaryKey.StartsWith(primaryKeyStartsWith));

                if (listOfKeys.Any())
                {                    
                    // Remove range.  Dic is immutable: no lock needed.
                    newCache = oldCache.RemoveRange(listOfKeys);
                }
                else
                {
                    newCache = oldCache;
                }
                 
                // compares oldCache with newCache - if these are now the s
                if (oldCache == Interlocked.CompareExchange(ref _cache, newCache, oldCache))
                {
                    // we can get out of the loop
                    return Task.FromResult(true);
                }

                // CompareExchange failed => another thread has made a change to _cache.
                // We need to do the add again, as we'll want to make sure we always work 
                // on the latest version - we don't want to loose changes to the cache.

            } while (true);

        }

        public Task ClearAsync()
        {
            _cache = _cache.Clear();
            return Task.FromResult(true);
        }



    }
   
    
    //public class ImmutableInMemoryCacheStore : ICacheStore
    //{

    //    private IImmutableDictionary<string, HttpResponseMessage> _cache = ImmutableDictionary.Create<string, HttpResponseMessage>();

    //    // get an item from cache with key "key"
    //    public Task<HttpResponseMessage> GetAsync(string key)
    //    {
    //        key = key.ToLower();

    //        HttpResponseMessage value;
    //        if (_cache.TryGetValue(key, out value))
    //        {
    //            return Task.FromResult((HttpResponseMessage)value);
    //        }
    //        else
    //        {
    //            return Task.FromResult(default(HttpResponseMessage));
    //        }
   
    //    }

    //    // put an item with key "key" in cache or overwrite it
    //    public Task SetAsync(string key, HttpResponseMessage value)
    //    {
           
    //        do
    //        {
    //            key = key.ToLower();

    //            var oldCache = _cache;
    //            IImmutableDictionary<string, HttpResponseMessage> newCache;
                
    //            if (oldCache.ContainsKey(key))
    //            {
    //                // overwrite.  Dic is immutable: no lock needed.
    //                newCache = oldCache.SetItem(key, value);
    //            }
    //            else
    //            {
    //                // Add the value to cache dictionary.  Dic is immutable: no lock needed.
    //                newCache = oldCache.Add(key, value);
    //            }
                
    //            // newCache = new cache dic, containing value.  Check.
                
    //            // Interlocked.CompareExchange(ref _cache, newCache, oldCache): 
    //            //
    //            // => if _cache is the same as oldcache, then  replace
    //            // _cache by newCache.  This is an effective check: if _cache is no longer the
    //            // same as oldCache, another thread has made a change to _cache.  If that's the
    //            // case, we need to do the add again, as we'll want to make sure we always work 
    //            // on the latest version - we don't want to loose changes to the cache.
    //            //
    //            // Call checks for reference equality, not an overridden Equals => we need
    //            // this reference check, new instance = different reference.
    //            //
    //            // CompareExchange always returns the value in "location", eg the first 
    //            // parameter, BEFORE the exchange.  So, if we check that value (_cache before 
    //            // exchange) against the oldCache and if these are the same, add was succesful,
    //            // and thanks to the CompareExchange call, _cache is now set to newCache

    //            // compares oldCache with newCache - if these are now the s
    //            if (oldCache == Interlocked.CompareExchange(ref _cache, newCache, oldCache))
    //            {
    //                // we can get out of the loop

    //                return Task.FromResult(true);
    //            }

    //            // CompareExchange failed => another thread has made a change to _cache.
    //            // We need to do the add again, as we'll want to make sure we always work 
    //            // on the latest version - we don't want to loose changes to the cache.

    //        } while (true);

    //    }


    //    public Task RemoveAsync(string key)
    //    {

    //        do
    //        {
    //            key = key.ToLower();

    //            var oldCache = _cache;
    //            IImmutableDictionary<string, HttpResponseMessage> newCache;

    //            if (oldCache.ContainsKey(key))
    //            {
    //                // Remove.  Dic is immutable: no lock needed.
    //                newCache = oldCache.Remove(key);
    //            }
    //            else
    //            {                 
    //                newCache = oldCache;
    //            } 

    //            // compares oldCache with newCache - if these are now the s
    //            if (oldCache == Interlocked.CompareExchange(ref _cache, newCache, oldCache))
    //            {
    //                // we can get out of the loop

    //                return Task.FromResult(true);
    //            }

    //            // CompareExchange failed => another thread has made a change to _cache.
    //            // We need to do the add again, as we'll want to make sure we always work 
    //            // on the latest version - we don't want to loose changes to the cache.

    //        } while (true);

    //    }


    //    public Task RemoveRangeAsync(string keyStartsWith)
    //    { 

    //        do
    //        {
    //            keyStartsWith = keyStartsWith.ToLower();

    //            var oldCache = _cache;
    //            IImmutableDictionary<string, HttpResponseMessage> newCache;

    //            var listOfKeys = oldCache.Keys.Where(k => k.StartsWith(keyStartsWith));
                
    //            if (listOfKeys.Any())
    //            {
    //                // Remove range.  Dic is immutable: no lock needed.
    //                newCache = oldCache.RemoveRange(listOfKeys);
    //            }
    //            else
    //            {
    //                newCache = oldCache;
    //            }

    //            // compares oldCache with newCache - if these are now the s
    //            if (oldCache == Interlocked.CompareExchange(ref _cache, newCache, oldCache))
    //            {
    //                // we can get out of the loop
    //                return Task.FromResult(true);
    //            }

    //            // CompareExchange failed => another thread has made a change to _cache.
    //            // We need to do the add again, as we'll want to make sure we always work 
    //            // on the latest version - we don't want to loose changes to the cache.

    //        } while (true);

    //    }

    //    public Task ClearAsync()
    //    {
    //        _cache = _cache.Clear();
    //        return Task.FromResult(true);
    //    }

    //}
}
