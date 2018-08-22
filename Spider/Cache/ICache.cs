namespace Spider.Cache
{
    public interface ICache
    {
        bool ContainsKey(string key);

        object Get(string key);

        void Set(string key, object val);

        int Count();
    }
}
