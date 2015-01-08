using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Marvin.HttpCache.Store
{
    public interface ICacheStore<TKey, TValue>
    {
        System.Threading.Tasks.Task ClearAsync();
        System.Threading.Tasks.Task<TValue> GetAsync(TKey key);
        System.Threading.Tasks.Task SetAsync(TKey key, TValue value);
    }
}
