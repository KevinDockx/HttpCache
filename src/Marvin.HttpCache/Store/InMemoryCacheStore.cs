//using System;
//using System.Collections.Generic;
//using System.Collections.Immutable;
//using System.Linq;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;

//namespace Marvin.HttpCache.Store
//{

//    class Cache<TKey, TValue>
//    {
//        private IImmutableDictionary<TKey, TValue> _cache = ImmutableDictionary.Create<TKey, TValue>();

//        public TValue GetOrAdd(TKey key, [NotNull] Func<TKey, TValue> valueFactory)
//        {
//            valueFactory.CheckArgumentNull("valueFactory");

//            TValue newValue = default(TValue);
//            bool newValueCreated = false;
//            while (true)
//            {
//                var oldCache = _cache;
//                TValue value;
//                if (oldCache.TryGetValue(key, out value))
//                    return value;

//                // Value not found; create it if necessary
//                if (!newValueCreated)
//                {
//                    newValue = valueFactory(key);
//                    newValueCreated = true;
//                }

//                // Add the new value to the cache
//                var newCache = oldCache.Add(key, newValue);
//                if (Interlocked.CompareExchange(ref _cache, newCache, oldCache) == oldCache)
//                {
//                    // Cache successfully written
//                    return newValue;
//                }

//                // Failed to write the new cache, try again
//            }
//        }

//        public void Clear()
//        {
//            _cache = _cache.Clear();
//        }
//    }


//    public class InMemoryCacheStore : ICacheStore
//    {
//       // http://stackoverflow.com/questions/18367839/alternative-to-concurrentdictionary-for-portable-class-library
//        private readonly ConcurrentDictionary<string, object> cachedResponses = new ConcurrentDictionary<string, object>();

//        public Task<T> GetAsync<T>(string key)
//        {
//            object value;

//            return cachedResponses.TryGetValue(key, out value
//                ) ? Task.FromResult((T)value) : Task.FromResult(default(T));
//        }

//        public Task SetAsync<T>(string key, T value)
//        {
//            cachedResponses[key] = value;

//            return Task.FromResult(true);
//        }

//        public Task ClearAsync()
//        {
//            cachedResponses.Clear();

//            return Task.FromResult(true);

//        }
//    }
//}
