using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Marvin.HttpCache.Store
{
    public class ImmutableInMemoryVaryByHeadersStore : IVaryByHeadersStore
    {
        private IImmutableDictionary<string, IEnumerable<string>> _storeDictionary = ImmutableDictionary.Create<string, IEnumerable<string>>();

        // get an item from store with key "key"
        public Task<IEnumerable<string>> GetAsync(string key)
        {
            key = key.ToLower();

            IEnumerable<string> value;
            if (_storeDictionary.TryGetValue(key, out value))
            {
                return Task.FromResult((IEnumerable<string>)value);
            }
            else
            {
                return Task.FromResult(default(IEnumerable<string>));
            }

        }

        // put an item with key "key" in store or overwrite it
        public Task SetAsync(string key, IEnumerable<string> value)
        {

            do
            {
                key = key.ToLower();

                var oldStore = _storeDictionary;
                IImmutableDictionary<string, IEnumerable<string>> newStore;

                if (oldStore.ContainsKey(key))
                {
                    // overwrite.  Dic is immutable: no lock needed.
                    newStore = oldStore.SetItem(key, value);
                }
                else
                {
                    // Add the value to dictionary.  Dic is immutable: no lock needed.
                    newStore = oldStore.Add(key, value);
                }
                         
                if (oldStore == Interlocked.CompareExchange(ref _storeDictionary, newStore, oldStore))
                {
                    // we can get out of the loop

                    return Task.FromResult(true);
                }

                // CompareExchange failed => another thread has made a change to _storeDictionary.
                // We need to do the add again, as we'll want to make sure we always work 
                // on the latest version - we don't want to loose changes to the store.

            } while (true);

        }


        public Task RemoveAsync(string key)
        {

            do
            {
                key = key.ToLower();

                var oldStore = _storeDictionary;
                IImmutableDictionary<string, IEnumerable<string>> newStore;

                if (oldStore.ContainsKey(key))
                {
                    // Remove.  Dic is immutable: no lock needed.
                    newStore = oldStore.Remove(key);
                }
                else
                {
                    newStore = oldStore;
                } 
              
                if (oldStore == Interlocked.CompareExchange(ref _storeDictionary, newStore, oldStore))
                {
                    // we can get out of the loop

                    return Task.FromResult(true);
                }

                
            } while (true);

        }

        public Task ClearAsync()
        {
            _storeDictionary = _storeDictionary.Clear();
            return Task.FromResult(true);
        }
    }
}
