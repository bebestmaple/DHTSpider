using System.Collections.Generic;
using System.Net;
using Tancoder.Torrent;

namespace Spider.Queue
{
    public interface IQueue
    {
        void Enqueue(KeyValuePair<InfoHash, IPEndPoint> item);

        KeyValuePair<InfoHash, IPEndPoint> Dequeue();

        int Count();
    }
}
