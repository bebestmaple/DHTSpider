using System.Linq;
using System.Runtime.Caching;

namespace Spider.Cache
{
    public class DefaultCache : ICache
    {
        public DefaultCache()
        {
        }

        public object Get(string key)
        {
            return MemoryCache.Default.Get(key);
        }

        public void Set(string key, object val)
        {
            MemoryCache.Default.Set(key, val, new CacheItemPolicy());
        }

        public bool ContainsKey(string key)
        {
            return MemoryCache.Default.Contains(key);
        }

        public int Count()
        {
            return MemoryCache.Default.Count();
        }
    }
}
