#if !DISABLE_DHT
using System;

namespace Tancoder.Torrent.Dht
{
    public class NodeAddedEventArgs : EventArgs
    {
        private Node node;

        public Node Node
        {
            get { return node; }
        }

        public NodeAddedEventArgs(Node node)
        {
            this.node = node;
        }
    }
}
#endif