using Socket_Quartz_Manage.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Socket_Quartz_Manage.SocketBase.SocketService
{
    public class SocketServerManager : ISocketServerManager
    {
        //private const string Receive_Saea = "Receive_Saea";
        private int m_numConnections; //客户端最大连接数
        private int m_receiveBufferSize; //单个消息接收数据大小
        BufferManager m_bufferManager;
        const int opsToPreAlloc = 2;
        Socket listenSocket; //服务端Socket实例
        Socket UserTokenSocket;

        SocketAsyncEventArgsPool m_readWritePool;
        int m_totalBytesRead; //当前服务器端读取数据总数
        int m_numConnectedSockets; //当前连接的客户端Socket数
        Semaphore m_maxNumberAcceptedClients;
        Dictionary<string, Socket> dic_AcceptSocket; //储存客户端Socket实例

        public IReadOnlyDictionary<IPAddress, Action<byte[], SocketAsyncEventArgs>> dic_receiveInvoke { get; set; } //储存客户端对应的接收消息处理方法委托

        private readonly IServiceProvider _serviceProvider;
        public SocketServerManager(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// 初始化
        /// </summary>
        public void Init(int numConnections, int ReceiveBufferSize, Dictionary<IPAddress, Action<byte[], SocketAsyncEventArgs>> dicreceiveInvoke)
        {
            m_totalBytesRead = 0;
            m_numConnectedSockets = 0;
            m_numConnections = numConnections;
            m_receiveBufferSize = ReceiveBufferSize;
            dic_receiveInvoke = dicreceiveInvoke;

            m_bufferManager = new BufferManager(ReceiveBufferSize * numConnections * opsToPreAlloc, ReceiveBufferSize);
            m_readWritePool = new SocketAsyncEventArgsPool(numConnections);
            m_maxNumberAcceptedClients = new Semaphore(numConnections, numConnections);

            m_bufferManager.InitBuffer();
            dic_AcceptSocket = new Dictionary<string, Socket>();
            SocketAsyncEventArgs readWriteEventArg;

            for (int i = 0; i < m_numConnections; i++)
            {
                readWriteEventArg = new SocketAsyncEventArgs();
                //readWriteEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);
                readWriteEventArg.UserToken = new AsyncUserToken();

                m_bufferManager.SetBuffer(readWriteEventArg);
                m_readWritePool.Push(readWriteEventArg);
            }
        }

        /// <summary>
        /// 服务启动
        /// </summary>
        /// <param name="localEndPoint"></param>
        public void start(IPEndPoint localEndPoint)
        {
            listenSocket = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            listenSocket.Bind(localEndPoint);

            listenSocket.Listen(100);

            SocketAsyncEventArgs acceptEventArg = new SocketAsyncEventArgs();
            acceptEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(AcceptEventArg_Completed);
            StartAccept(acceptEventArg);

            Console.WriteLine("Press any key to terminate the server process.....");
        }

        /// <summary>
        /// 开始等待客户端连接
        /// </summary>
        /// <param name="acceptEventArg"></param>
        public void StartAccept(SocketAsyncEventArgs acceptEventArg)
        {
            bool willRaiseEvent = false;
            while (!willRaiseEvent)
            {
                m_maxNumberAcceptedClients.WaitOne(); //信号量控制线程数量，限制并发

                acceptEventArg.AcceptSocket = null;
                willRaiseEvent = listenSocket.AcceptAsync(acceptEventArg); //如果true，自动调用Completed事件，false，则需要手动执行ProcessAccept处理
                if (!willRaiseEvent)
                {
                    ProcessAccept(acceptEventArg);
                }

            }
        }

        void AcceptEventArg_Completed(object sender, SocketAsyncEventArgs e)
        {
            ProcessAccept(e); //处理连接成功的socket连接

            StartAccept(e); //等待下一个socket客户端连接
        }

        private void ProcessAccept(SocketAsyncEventArgs e)
        {
            Interlocked.Increment(ref m_numConnectedSockets); //当前Socket连接数线程安全递增
            Console.WriteLine("Client {0} connection accepted. There are {1} clients connected to the server", e.AcceptSocket.RemoteEndPoint.ToString(), m_numConnectedSockets);

            var ip_address = ((IPEndPoint)e.AcceptSocket.RemoteEndPoint).Address.ToString();

            var receive_invoke = dic_receiveInvoke[IPAddress.Parse(ip_address)];
            SocketAsyncEventArgs readEventArgs = m_readWritePool.Pop();
            ((AsyncUserToken)readEventArgs.UserToken).Socket = e.AcceptSocket;
            readEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(IO_ReceiveCompleted); //绑定接收成功事件


            dic_AcceptSocket.Add(ip_address, e.AcceptSocket);
            bool willRaiseEvent = e.AcceptSocket.ReceiveAsync(readEventArgs);
            if (!willRaiseEvent)
            {
                ProcessReceive(readEventArgs, receive_invoke);
            }
            //用于绑定事件
            void IO_ReceiveCompleted(object sender, SocketAsyncEventArgs e)
            {
                try
                {
                    ProcessReceive(e, receive_invoke);
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }


        /// <summary>
        /// 消息接收回调
        /// </summary>
        /// <param name="e"></param>
        /// <param name="action"></param>
        private void ProcessReceive(SocketAsyncEventArgs e, Action<byte[], SocketAsyncEventArgs> action)
        {
            AsyncUserToken token = (AsyncUserToken)e.UserToken; //获取UserToken，即客户端连接
            if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
            {
                Interlocked.Add(ref m_totalBytesRead, e.BytesTransferred);
                Console.WriteLine("The server has read a total of {0} bytes", m_totalBytesRead);
                int lengthBuffer = e.BytesTransferred;
                if (lengthBuffer > 0)
                {
                    byte[] receiveBuffer = e.Buffer.Skip(e.Offset).Take(lengthBuffer).ToArray();
                    byte[] buffer = new byte[lengthBuffer];
                    Buffer.BlockCopy(receiveBuffer, 0, buffer, 0, lengthBuffer);
                    action(buffer, e); //执行自定义处理
                    bool willRaiseEvent = token.Socket.ReceiveAsync(e); //继续异步接收消息
                    if (!willRaiseEvent)
                    {
                        ProcessReceive(e, action);
                    }
                }
            }
            else
            {
                CloseClientSocket(e);
            }
        }

        /// <summary>
        /// 消息发送回调
        /// </summary>
        /// <param name="e"></param>
        private void ProcessSend(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                AsyncUserToken token = (AsyncUserToken)e.UserToken;
                //bool willRaiseEvent = token.Socket.ReceiveAsync(e);
                //if (!willRaiseEvent)
                //{
                //    ProcessReceive(e);
                //}
                int lengthBuffer = e.BytesTransferred;
                if (lengthBuffer > 0)
                {
                    byte[] receiveBuffer = e.Buffer.Skip(e.Offset).Take(lengthBuffer).ToArray();
                    byte[] buffer = new byte[lengthBuffer];
                    Buffer.BlockCopy(receiveBuffer, 0, buffer, 0, lengthBuffer);
                    Console.WriteLine("Send Success" + buffer.ToHexStrFromByte());
                }

                m_readWritePool.Push(e);  //回收
            }
            else
            {
                CloseClientSocket(e);
            }
        }

        /// <summary>
        /// 连接关闭，资源释放
        /// </summary>
        /// <param name="e"></param>
        private void CloseClientSocket(SocketAsyncEventArgs e)
        {
            AsyncUserToken token = e.UserToken as AsyncUserToken;
            try
            {
                token.Socket.Shutdown(SocketShutdown.Send);
            }
            catch (Exception)
            {
            }
            var ip_address = ((IPEndPoint)token.Socket.RemoteEndPoint).Address.ToString();
            dic_AcceptSocket.Remove(ip_address);
            token.Socket.Close(); //Socket连接关闭

            Interlocked.Decrement(ref m_numConnectedSockets); //当前Socket连接数线程安全递减

            m_readWritePool.Push(e); //Sokcet事件处理池新增

            m_maxNumberAcceptedClients.Release(); //信号量释放
            Console.WriteLine("A Client has been disconnected from the SocketServerManager. There are {0} clients connected to the server", m_numConnectedSockets);
        }


        /// <summary>
        /// 消息发送
        /// </summary>
        /// <param name="ip_address"></param>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public async Task SendAsync(string ip_address, byte[] bytes)
        {
            if (!dic_AcceptSocket.ContainsKey(ip_address))
            {
                Console.WriteLine("cannot found SocketAsyncEventArgs in dictionary!");
                return;
            }
            SocketAsyncEventArgs saea = m_readWritePool.Pop();
            ((AsyncUserToken)saea.UserToken).Socket = dic_AcceptSocket[ip_address];
            saea.Completed += new EventHandler<SocketAsyncEventArgs>(IO_SendCompleted);

            saea.SetBuffer(bytes, 0, bytes.Length);
            bool willRaiseEvent = dic_AcceptSocket[ip_address].SendAsync(saea);
            if (!willRaiseEvent)
            {
                ProcessSend(saea);
            }
        }

        //用于绑定事件
        void IO_SendCompleted(object sender, SocketAsyncEventArgs e)
        {
            try
            {
                ProcessSend(e);
            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}
