using Autofac;
using Spider.Cache;
using Spider.Core;
using Spider.Log;
using Spider.Queue;
using Spider.Store;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Tancoder.Torrent;
using Tancoder.Torrent.BEncoding;
using Tancoder.Torrent.Client;

namespace Spider
{
    public class SpiderConfiguration
    {
        private readonly ContainerBuilder _builder;
        private IContainer _container;

        private static readonly object obj = new object();
        private static SpiderConfiguration _instance = null;
        
        public SpiderSetting _option { get; set; }
        public ICache _cache { get; set; }
        public IQueue _queue { get; set; }
        public IStore _store { get; set; }

        private SpiderConfiguration(SpiderSetting option)
        {
            _option = option;
            _builder = new ContainerBuilder();

            if (_option.IsSaveTorrent)
            {
                if (string.IsNullOrEmpty(_option.TorrentSavePath) || !Directory.Exists(_option.TorrentSavePath))
                {
                    _option.TorrentSavePath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "torrent");
                    Directory.CreateDirectory(_option.TorrentSavePath);
                }
            }
        }

        public static SpiderConfiguration Create(SpiderSetting option = null)
        {
            if (_instance == null)
            {
                lock (obj)
                {
                    if (_instance == null)
                    {
                        _instance = new SpiderConfiguration(option ?? new SpiderSetting());
                    }
                }
            }
            return _instance;
        }

        /// <summary>
        /// 使用默认内存缓存
        /// </summary>
        /// <returns></returns>
        public SpiderConfiguration UseDefaultCache()
        {
            _builder.RegisterType<DefaultCache>().As<ICache>().Named<ICache>("Cache").SingleInstance();
            return _instance;
        }

        /// <summary>
        /// 使用Redis缓存
        /// </summary>
        /// <returns></returns>
        public SpiderConfiguration UseRedisCache()
        {
            _builder.RegisterType<RedisCache>().As<ICache>().Named<ICache>("Cache").SingleInstance();
            return _instance;
        }

        /// <summary>
        /// 使用默认内存队列
        /// </summary>
        /// <returns></returns>
        public SpiderConfiguration UseDefaultQueue()
        {
            _builder.RegisterType<DefaultQueue>().As<IQueue>().Named<IQueue>("Queue").SingleInstance();
            return _instance;
        }

        /// <summary>
        /// 使用Redis队列
        /// </summary>
        /// <returns></returns>
        public SpiderConfiguration UseRedisQueue()
        {
            _builder.RegisterType<RedisQueue>().As<IQueue>().Named<IQueue>("Queue").SingleInstance();
            return _instance;
        }

        /// <summary>
        /// 使用ElasticSearch存储
        /// </summary>
        /// <returns></returns>
        public SpiderConfiguration UseElasticSearchStore()
        {
            _builder.RegisterType<ElasticSearchStore>().As<IStore>().Named<IStore>("Store").SingleInstance();
            return _instance;
        }

        /// <summary>
        /// 使用MongoDB存储
        /// </summary>
        /// <returns></returns>
        public SpiderConfiguration UseMongoDBStore()
        {
            _builder.RegisterType<MongoDBStore>().As<IStore>().Named<IStore>("Store").SingleInstance();
            return _instance;
        }

        public void Stop()
        {
            //TODO
        }

        public SpiderConfiguration Start()
        {
            _container = _builder.Build();
            if (_container.IsRegisteredWithName<ICache>("Cache"))
            {
                this.UseDefaultCache();
                _cache = _container.ResolveNamed<ICache>("Cache");
            }
            


            if (_container.IsRegisteredWithName<IQueue>("Queue"))
            {
                this.UseDefaultQueue();
               _queue = _container.ResolveNamed<IQueue>("Queue");
            }


            if (_container.IsRegisteredWithName<IStore>("Store"))
            {
                _store = _container.ResolveNamed<IStore>("Store");
            }


            for (var i = 0; i < _option.MaxSpiderThreadCount; i++)
            {
                var port = _option.LocalPort + i;
                WriteLog($"线程：{i + 1} 端口：{port} 已启动监听...");
                

                Task.Run(() =>
                {
                    var spider = new DHTSpider(new IPEndPoint(IPAddress.Parse("0.0.0.0"), port), _queue);
                    spider.NewMetadata += DHTSpider_NewMetadata;
                    spider.Start();
                });
                Thread.Sleep(1000);
            }

            for (var i = 0; i < _option.MaxDownLoadThreadCount; i++)
            {
                var id = i + 1;
                WriteLog($"线程[{id}]开始下载");
                Task.Run(() =>
                {
                    Download(id);
                });
                Thread.Sleep(100);
            }


            return _instance;
        }

        private void DHTSpider_NewMetadata(object sender, NewMetadataEventArgs e)
        {
            var hash = e.Metadata.ToString();
            lock (obj)
            {
                if (_cache != null && !_cache.ContainsKey(hash))
                {
                    _cache.Set(hash, e.Owner);
                    _queue.Enqueue(new KeyValuePair<InfoHash, IPEndPoint>(e.Metadata, e.Owner));


                    WriteLog($"NewMetadata    Hash:{e.Metadata}  Address:{e.Owner.ToString()}");
                }
            }

        }

        private void Download(int threadId)
        {
            while (true)
            {
                try
                {
                    KeyValuePair<InfoHash, IPEndPoint>? info = new KeyValuePair<InfoHash, IPEndPoint>();
                    lock (this)
                    {
                        info = _queue?.Dequeue();
                    }
                    if (!info.HasValue || info.Value.Key == null || info.Value.Value == null)
                    {
                        Thread.Sleep(500);
                        continue;
                    }
                    var hash = BitConverter.ToString(info.Value.Key.Hash).Replace("-", "");
                    using (WireClient client = new WireClient(info.Value.Value))
                    {
                        var metadata = client.GetMetaData(info.Value.Key);
                        if (metadata != null)
                        {
                            var name = ((BEncodedString)metadata["name"]).Text;
                            if (_option.IsSaveTorrent)
                            {
                                var filepath = $"{_option.TorrentSavePath}\\{hash}.torrent";
                                File.WriteAllBytes(filepath, metadata.Encode());
                            }
                            Logger.ConsoleWrite($"线程[{threadId}]下载完成   Name:{name} ", ConsoleColor.Yellow);
                        }
                    }

                }
                catch (Exception ex)
                {
                    Logger.Error($"Download {ex.Message}");
                }
            }
        }

        private void WriteLog(string msg, ConsoleColor consoleColor = ConsoleColor.Green)
        {
            if (_option.IsWriteToConsole)
            {
                Logger.ConsoleWrite(msg, consoleColor);
            }
        }
        
    }
}
