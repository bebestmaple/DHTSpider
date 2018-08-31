using Spider.Log;
using Spider.Queue;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Tancoder.Torrent;
using Tancoder.Torrent.BEncoding;
using Tancoder.Torrent.Dht;
using Tancoder.Torrent.Dht.Messages;

namespace Spider.Core
{
    public class DHTSpider : IDhtEngine
    {
        public DHTSpider(IPEndPoint localAddress, IQueue queue)
        {
            LocalAddress = localAddress;
            LocalId = NodeId.Create();
            udpSocketListener = new UdpSocketListener(localAddress);
            KTable = new ConcurrentDictionary<string, Node>();
            TokenManager = new EasyTokenManager();
            Queue = queue;
            MessageQueue = new ConcurrentQueue<KeyValuePair<IPEndPoint, byte[]>>();
        }

        #region  初始DHT网络节点
        /// <summary>
        /// 初始DHT网络节点
        /// </summary>
        private static List<IPEndPoint> BOOTSTRAP_NODES = new List<IPEndPoint>()
        {
            new IPEndPoint(Dns.GetHostEntry("router.bittorrent.com").AddressList[0], 6881),
            new IPEndPoint(Dns.GetHostEntry("dht.transmissionbt.com").AddressList[0], 6881),
            new IPEndPoint(Dns.GetHostEntry("router.utorrent.com").AddressList[0], 6881),
        };
        #endregion

        /// <summary>
        /// UDP监听器
        /// </summary>
        private UdpSocketListener udpSocketListener;

        /// <summary>
        /// 最大保存节点数
        /// </summary>
        private static int MaxNodesSize = int.MaxValue;

        /// <summary>
        /// 消息队列
        /// </summary>
        public ConcurrentQueue<KeyValuePair<IPEndPoint, byte[]>> MessageQueue;

        /// <summary>
        /// 同步锁
        /// </summary>
        private object locker = new object();


        private List<Task> _TaskList = new List<Task>();


        private CancellationTokenSource _CancellationTokenSource;

        public IMetaDataFilter Filter { get; set; }

        public IQueue Queue { get; set; }

        /// <summary>
        /// 自身NodeId
        /// </summary>
        public NodeId LocalId { get; set; }

        /// <summary>
        /// 本机地址
        /// </summary>
        public IPEndPoint LocalAddress { get; set; }

        public ITokenManager TokenManager { get; private set; }


        public ConcurrentDictionary<string, Node> KTable;

        public event NewMetadataEvent NewMetadata;


        #region IDisposable
        private bool disposed = false;
        public bool Disposed => disposed;
        public void Dispose()
        {
            if (disposed)
            {
                return;
            }
        }
        #endregion

        #region 向KTable添加节点
        /// <summary>
        /// 向KTable添加节点
        /// </summary>
        /// <param name="nodes"></param>
        public void Add(BEncodedList nodes)
        {
            Add(Node.FromCompactNode(nodes));
        }

        /// <summary>
        /// 向KTable添加节点
        /// </summary>
        /// <param name="nodes"></param>
        public void Add(IEnumerable<Node> nodes)
        {
            foreach (var node in nodes)
            {
                Add(node);
            }
        }

        /// <summary>
        /// 向KTable添加节点
        /// </summary>
        /// <param name="node"></param>
        public void Add(Node node)
        {
            if (KTable.Count >= MaxNodesSize)
            {
                return;
            }
            if (!KTable.AsParallel().Any(x => x.Key == node.Id.ToString()))
            {
                KTable.TryAdd(node.Id.ToString(), node);
            }
        }
        #endregion

        #region 在KTable中查找节点
        /// <summary>
        /// 在KTable中查找节点
        /// </summary>
        /// <param name="nid">节点ID</param>
        /// <returns></returns>
        public Node FindNode(NodeId nid)
        {
            var node = new Node(NodeId.Create(), LocalAddress);
            if (KTable.TryGetValue(nid.ToString(), out node))
            {
                return node;
            }
            return null;
        }
        #endregion
        

        public void GetAnnounced(InfoHash infohash, IPEndPoint endpoint)
        {
            try
            {
                if (Filter == null || (Filter != null && Filter.Ignore(infohash)))
                {
                    NewMetadata?.Invoke(this, new NewMetadataEventArgs(infohash, endpoint));
                }
            }
            catch
            {


            }
        }

        public NodeId GetNeighborId(NodeId target)
        {
            byte[] nid = new byte[target.Bytes.Length];
            Array.Copy(target.Bytes, nid, nid.Length / 2);
            Array.Copy(LocalId.Bytes, nid.Length / 2, nid, nid.Length / 2, nid.Length / 2);
            return new NodeId(nid);
        }

        public void GetPeers(InfoHash infohash)
        {

        }


        public FindPeersResult QueryFindNode(NodeId target)
        {

            var result = new FindPeersResult();
            var targetNode = FindNode(target);
            if (targetNode != null)
            {
                result.Nodes.Add(targetNode);
            }
            else
            {
                result.Nodes = GetClosestFromKTable(target);
            }
            return result;
        }

        public GetPeersResult QueryGetPeers(NodeId infohash)
        {
            var result = new GetPeersResult()
            {
                HasHash = false,
                Nodes = new List<Node>(),
            };
            return result;
        }


        /// <summary>
        /// 获得距离目标节点最近的节点列表
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
		public List<Node> GetClosestFromKTable(NodeId target)
        {
            SortedList<NodeId, Node> sortedNodes = new SortedList<NodeId, Node>(8);


            foreach (Node n in KTable.Values)
            {
                NodeId distance = n.Id.Xor(target);
                if (sortedNodes.Count == 8)
                {
                    if (distance > sortedNodes.Keys[sortedNodes.Count - 1])//maxdistance
                    {
                        continue;
                    }

                    //remove last (with the maximum distance)
                    sortedNodes.RemoveAt(sortedNodes.Count - 1);
                }
                sortedNodes.Add(distance, n);
            }
            return new List<Node>(sortedNodes.Values);
        }

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="msg">消息体</param>
        /// <param name="endpoint">目标IP</param>
        public void Send(DhtMessage msg, IPEndPoint endpoint)
        {
            if (msg.TransactionId == null)
            {
                if (msg is ResponseMessage)
                {
                    //throw new ArgumentException("Message must have a transaction id");
                }
                msg.TransactionId = TransactionId.NextId();
            }
            var buffer = msg.Encode();

            udpSocketListener.Send(buffer, endpoint);
        }

        /// <summary>
        /// 开始爬虫任务
        /// </summary>
        public void Start()
        {
            udpSocketListener.Start();
            udpSocketListener.MessageReceived += OnMessageReceived;


            if (_TaskList == null)
            {

                _TaskList = new List<Task>();
            }

            _CancellationTokenSource = new CancellationTokenSource();

            _TaskList.Add(Task.Run(() =>
            {
                while (true)
                {
                    if (!_CancellationTokenSource.IsCancellationRequested && Queue.Count() <= 0)
                    {
                        JoinDHTNetwork();
                        MakeNeighbours();
                    }
                }

            }, _CancellationTokenSource.Token));

            for (int i = 0; i < 10; i++)
            {
                _TaskList.Add(Task.Run(() =>
                    {
                        ProcessMessage();
                    }, _CancellationTokenSource.Token));
            }

        }

        /// <summary>
        /// 停止爬虫任务
        /// </summary>
        public void Stop()
        {
            udpSocketListener.Stop();
            _CancellationTokenSource.Cancel();

            _TaskList.AsParallel().ForAll(task =>
            {
                while (task.IsCanceled || task.IsCompleted || task.IsFaulted)
                {
                    task.Dispose();
                }

            });
        }

        #region 向初始DHT节点发送消息，以将本节点加入DHT网络
        /// <summary>
        /// 向初始DHT节点发送消息，以将本节点加入DHT网络
        /// </summary>
        private void JoinDHTNetwork()
        {
            BOOTSTRAP_NODES.AsParallel().ForAll(ipEndPoint =>
            {
                SendFindNodeRequest(ipEndPoint);
            });
        }
        #endregion


        /// <summary>
        /// 向所有节点发送 Find Node 消息
        /// </summary>
        private void MakeNeighbours()
        {
            KTable.AsParallel().ForAll(item =>
            {
                SendFindNodeRequest(item.Value.EndPoint, item.Value.Id);
            });

            KTable.Clear();
        }


        /// <summary>
        /// 发送 Find Node 消息
        /// </summary>
        /// <param name="address">IP地址</param>
        /// <param name="nodeid">节点Id</param>
        private void SendFindNodeRequest(IPEndPoint address, NodeId nodeid = null)
        {
            var nid = nodeid == null ? LocalId : GetNeighborId(nodeid);
            try
            {
                var msg = new FindNode(nid, NodeId.Create());
                Send(msg, address);
            }
            catch (Exception ex)
            {
                Logger.Trace($"SendFindNodeRequest nid:{nid} {address} {ex.ToString()}");
            }
        }


        /// <summary>
        /// UDP监听器 接收到消息后的处理
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="endpoint"></param>
        private void OnMessageReceived(byte[] buffer, IPEndPoint endpoint)
        {
            try
            {
                MessageQueue.Enqueue(new KeyValuePair<IPEndPoint, byte[]>(endpoint, buffer));
            }
            catch (Exception ex)
            {
                Logger.Fatal($"OnMessageReceived :{ex.ToString()}");
            }
        }

        private void ProcessMessage()
        {
            while (true)
            {
                if (!_CancellationTokenSource.IsCancellationRequested && MessageQueue.Count > 0)
                {

                    var msg = new KeyValuePair<IPEndPoint, byte[]>();
                    if (MessageQueue.TryDequeue(out msg))
                    {
                        ProcessMessage(msg.Value, msg.Key);
                    }
                }
            }
        }

        private void ProcessMessage(byte[] buffer, IPEndPoint endpoint)
        {
            try
            {
                DhtMessage message;
                string error;
                if (MessageFactory.TryNoTraceDecodeMessage((BEncodedDictionary)BEncodedValue.Decode(buffer, 0, buffer.Length, false), out message, out error))
                {
                    if (message is QueryMessage)
                    {
                        message.Handle(this, new Node(message.Id, endpoint));
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Fatal($"ProcessMessage {ex}");
            }
        }
    }
}
