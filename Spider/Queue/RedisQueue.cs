using Spider.Helper;
using System.Collections.Generic;
using System.Net;
using Tancoder.Torrent;

namespace Spider.Queue
{
    public class RedisQueue : IQueue
    {
        private const string RedisQueueKey = "RedisQueueKey";

        public RedisQueue()
        {

        }


        public  KeyValuePair<InfoHash, IPEndPoint> Dequeue()
        {
            return RedisHelper.Instance.ListLeftPop<KeyValuePair<InfoHash, IPEndPoint>>(RedisQueueKey);
        }

       

        public void Enqueue(KeyValuePair<InfoHash, IPEndPoint> item)
        {
            RedisHelper.Instance.ListRightPush<KeyValuePair<InfoHash, IPEndPoint>>(RedisQueueKey,item);
        }


        public int Count()
        {
            return (int)RedisHelper.Instance.ListLength(RedisQueueKey);
        }
        

        


    }
}
