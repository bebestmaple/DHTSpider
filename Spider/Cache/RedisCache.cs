﻿using System;

namespace Spider.Cache
{
    public class RedisCache : ICache
    {
        
        public RedisCache()
        {
        }

        public bool ContainsKey(string key)
        {
            throw new NotImplementedException();
        }

        public int Count()
        {
            throw new NotImplementedException();
        }

        public object Get(string key)
        {
            throw new NotImplementedException();
        }

        public void Set(string key, object val)
        {
            throw new NotImplementedException();
        }
    }
}
