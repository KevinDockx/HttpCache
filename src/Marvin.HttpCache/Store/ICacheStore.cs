using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;

namespace Marvin.HttpCache.Store
{
    public interface ICacheStore 
    {
        System.Threading.Tasks.Task ClearAsync();
        System.Threading.Tasks.Task<HttpResponseMessage> GetAsync(string key);
        System.Threading.Tasks.Task SetAsync(string key, HttpResponseMessage value);
        System.Threading.Tasks.Task RemoveAsync(string key);
        System.Threading.Tasks.Task RemoveRangeAsync(string keyStartsWith);

    }
}
