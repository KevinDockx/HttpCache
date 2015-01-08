using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Marvin.HttpCache.Store
{
    public class ImmutableInMemoryCacheStore<TKey, TValue> : ICacheStore<TKey, TValue>
    {

        private IImmutableDictionary<TKey, TValue> _cache = ImmutableDictionary.Create<TKey, TValue>();

        // get an item from cache with key "key"
        public Task<TValue> GetAsync(TKey key)
        {
            TValue value;
            if (_cache.TryGetValue(key, out value))
            {
                return Task.FromResult((TValue)value);
            }
            else
            {
                return Task.FromResult(default(TValue));
            }
   
        }

        // put an item with key "key" in cache or overwrite it
        public Task SetAsync(TKey key, TValue value)
        {
           
            do
            {
                var oldCache = _cache;
                IImmutableDictionary<TKey, TValue> newCache;
                
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
        
        public Task ClearAsync()
        {
            _cache = _cache.Clear();
            return Task.FromResult(true);
        }

    }
}
