namespace Spider
{
    public class SpiderSetting
    {
        /// <summary>
        /// 端口
        /// </summary>
        public int LocalPort { get; set; }

        public bool IsWriteToConsole { get; set; }

        /// <summary>
        /// 是否将日志记录到文件
        /// </summary>
        public bool IsLogInFile { get; set; }

        /// <summary>
        /// 是否保存种子文件
        /// </summary>
        public bool IsSaveTorrent { get; set; }

        /// <summary>
        /// 种子文件保存路径
        /// </summary>
        public string TorrentSavePath { get; set; }

        /// <summary>
        /// 最大下载线程数
        /// </summary>
        public int MaxDownLoadThreadCount { get; set; }

        /// <summary>
        /// 爬虫最大线程数
        /// </summary>
        public int MaxSpiderThreadCount { get; set; }

        /// <summary>
        /// 初始化DHT爬虫参数
        /// </summary>
        /// <param name="localPort">本地开放DHT端口，默认：6881</param>
        /// <param name="isSaveTorrent">是否保存种子文件，默认：false</param>
        /// <param name="torrentSavePath">种子文件保存路径，默认为空</param>
        /// <param name="maxDownLoadThreadCount">最大下载线程数，默认：10</param>
        /// <param name="maxSpiderThreadCount">爬虫最大线程数，默认：1</param>
        public SpiderSetting(int localPort = 6881, bool isSaveTorrent = false, string torrentSavePath = "", int maxDownLoadThreadCount = 10, int maxSpiderThreadCount = 1,bool isLogInFile = true,bool isWriteToConsole = true)
        {
            LocalPort = localPort;
            IsSaveTorrent = isSaveTorrent;
            TorrentSavePath = torrentSavePath;
            MaxDownLoadThreadCount = maxDownLoadThreadCount;
            MaxSpiderThreadCount = maxSpiderThreadCount;
            IsLogInFile = isLogInFile;
            IsWriteToConsole = isWriteToConsole;
        }
        

    }
}
