using Spider.Log;
using System;
using System.Net;
using System.Net.Sockets;

namespace Spider.Core
{
    public delegate void MessageReceived(byte[] buffer, IPEndPoint endpoint);

    public class UdpSocketListener
    {
        private IPEndPoint endpoint;

        /// <summary>
        /// 监听IP地址
        /// </summary>
        public IPEndPoint Endpoint
        {
            get { return endpoint; }
        }

        /// <summary>
        /// 监听Socket
        /// </summary>
        private Socket m_ListenSocket;

        private SocketAsyncEventArgs m_ReceiveSAE;

        #region 构造函数
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="endpoint"></param>
        public UdpSocketListener(IPEndPoint endpoint)
        {
            this.endpoint = endpoint;
        } 
        #endregion


        /// <summary>
        /// 开始监听
        /// </summary>
        public void Start()
        {
            try
            {
                #region 初始化Socket
                m_ListenSocket = new Socket(this.endpoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
                m_ListenSocket.Ttl = 255;
                m_ListenSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                m_ListenSocket.Bind(this.endpoint); 
                #endregion
                
                {
                    uint IOC_IN = 0x80000000;
                    uint IOC_VENDOR = 0x18000000;
                    uint SIO_UDP_CONNRESET = IOC_IN | IOC_VENDOR | 12;

                    byte[] optionInValue = { Convert.ToByte(false) };
                    byte[] optionOutValue = new byte[4];
                    m_ListenSocket.IOControl((int)SIO_UDP_CONNRESET, optionInValue, optionOutValue);
                }

                var eventArgs = new SocketAsyncEventArgs();
                m_ReceiveSAE = eventArgs;

                eventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(eventArgs_Completed);
                eventArgs.RemoteEndPoint = new IPEndPoint(IPAddress.Any, 0);

                int receiveBufferSize = 2048;
                var buffer = new byte[receiveBufferSize];
                eventArgs.SetBuffer(buffer, 0, buffer.Length);

                m_ListenSocket.ReceiveFromAsync(eventArgs);
            }
            catch (Exception ex)
            {
                OnError(ex);
            }
        }

        public event MessageReceived MessageReceived;
        private void eventArgs_Completed(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError != SocketError.Success)
            {
                Logger.Fatal($"errorCode:{(int)e.SocketError}");
            }

            if (e.LastOperation == SocketAsyncOperation.ReceiveFrom)
            {
                try
                {
                    //获取接收到的数据
                    byte[] ByteArray = new byte[e.BytesTransferred];
                    Array.Copy(e.Buffer, 0, ByteArray, 0, ByteArray.Length);
                    MessageReceived?.Invoke(ByteArray, (IPEndPoint)e.RemoteEndPoint);
                }
                catch (Exception exc)
                {
                    OnError(exc);
                }

                try
                {
                    m_ListenSocket.ReceiveFromAsync(e);
                }
                catch (Exception exc)
                {
                    OnError(exc);
                }
            }
        }

        private void OnError(Exception ex)
        {
            Logger.Fatal(ex.Message + ex.StackTrace);
        }

        /// <summary>
        /// 停止监听
        /// </summary>
        public void Stop()
        {
            if (m_ListenSocket == null)
                return;

            lock (this)
            {
                if (m_ListenSocket == null)
                    return;

                m_ReceiveSAE.Completed -= new EventHandler<SocketAsyncEventArgs>(eventArgs_Completed);
                m_ReceiveSAE.Dispose();
                m_ReceiveSAE = null;
                
                {
                    try
                    {
                        m_ListenSocket.Shutdown(SocketShutdown.Both);
                    }
                    catch { }
                }

                try
                {
                    m_ListenSocket.Close();
                }
                catch { }
                finally
                {
                    m_ListenSocket = null;
                }
            }

        }

        /// <summary>
        /// 发送数据包
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="endpoint"></param>
        public void Send(byte[] buffer, IPEndPoint endpoint)
        {
            try
            {
                if (endpoint.Address != IPAddress.Any)
                {
                    var len = m_ListenSocket.SendTo(buffer, endpoint);
                }
                else
                {
                    Logger.Fatal($"Send Not Work {endpoint.ToString()}");
                }
            }
            catch (Exception ex)
            {
                OnError(ex);
            }
        }

    }
}
