using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Marvin.HttpCache.Store
{
    public interface IVaryByHeadersStore
    {
        System.Threading.Tasks.Task ClearAsync();
        System.Threading.Tasks.Task<IEnumerable<string>> GetAsync(string key);
        System.Threading.Tasks.Task SetAsync(string key, IEnumerable<string> value);
        System.Threading.Tasks.Task RemoveAsync(string key);
    }
}
