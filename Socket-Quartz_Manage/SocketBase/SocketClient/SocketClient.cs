using Socket_Quartz_Manage.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Socket_Quartz_Manage.SocketBase.SocketClient
{
    public class SocketClient
    {

        Socket clientSocket; //服务端Socket实例
        Socket UserTokenSocket;

        SocketAsyncEventArgsPool m_readWritePool;
        Semaphore m_maxNumberAcceptedClients;
        int m_totalBytesRead; //当前服务器端读取数据总数
        int m_numConnectedSockets; //当前连接的客户端Socket数

        Action<byte[], SocketAsyncEventArgs> current_handle; //当前socket客户端消息处理
        public SocketClient(SocketAsyncEventArgsPool socketAsyncEventArgsPool, Semaphore semaphore, ref int _m_totalBytesRead, ref int _m_numConnectedSockets)
        {
            m_readWritePool = socketAsyncEventArgsPool;
            m_maxNumberAcceptedClients = semaphore;
            m_totalBytesRead = _m_totalBytesRead;
            m_numConnectedSockets = _m_numConnectedSockets;
        }

        /// <summary>
        /// 服务启动
        /// </summary>
        /// <param name="localEndPoint"></param>
        public void start(IPEndPoint serverEndPoint, Action<byte[], SocketAsyncEventArgs> handle)
        {
            current_handle = handle;
            clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            SocketAsyncEventArgs acceptEventArg = new SocketAsyncEventArgs();
            acceptEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(EventArgConnected_Completed);
            acceptEventArg.RemoteEndPoint = serverEndPoint;
            StartAccept(acceptEventArg);

            Console.WriteLine("Press any key to terminate the server process.....");
        }

        /// <summary>
        /// 开始等待客户端连接
        /// </summary>
        /// <param name="acceptEventArg"></param>
        private void StartAccept(SocketAsyncEventArgs acceptEventArg)
        {
            bool willRaiseEvent = false;
            //while (!willRaiseEvent)
            //{
            m_maxNumberAcceptedClients.WaitOne(); //信号量控制线程数量，限制并发

            acceptEventArg.AcceptSocket = null;
            willRaiseEvent = clientSocket.ConnectAsync(acceptEventArg); //如果true，自动调用Completed事件，false，则需要手动执行ProcessConnect处理
            if (!willRaiseEvent)
            {
                ProcessConnect(acceptEventArg);
            }

            //}
        }

        void EventArgConnected_Completed(object sender, SocketAsyncEventArgs e)
        {
            ProcessConnect(e); //处理连接成功的socket连接

            //StartAccept(e); //等待下一个socket客户端连接
        }

        private void ProcessConnect(SocketAsyncEventArgs e)
        {
            try
            {
                if (e.ConnectSocket == null)  //连接失败
                {
                    StartAccept(e); //重新开始连接
                    return;
                }
                Interlocked.Increment(ref m_numConnectedSockets); //当前Socket连接数线程安全递增
                Console.WriteLine("Server {0} connection Connected. There are {1} clients connected in SocketClientManager", e.ConnectSocket.RemoteEndPoint.ToString(), m_numConnectedSockets);

                var ip_address = ((IPEndPoint)e.ConnectSocket.RemoteEndPoint).Address.ToString();

                SocketAsyncEventArgs readEventArgs = m_readWritePool.Pop();
                ((AsyncUserToken)readEventArgs.UserToken).Socket = e.ConnectSocket;
                readEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(IO_ReceiveCompleted); //绑定接收成功事件


                UserTokenSocket = e.ConnectSocket;
                bool willRaiseEvent = e.ConnectSocket.ReceiveAsync(readEventArgs);
                if (!willRaiseEvent)
                {
                    ProcessReceive(readEventArgs, current_handle);
                }
                //用于绑定事件
                void IO_ReceiveCompleted(object sender, SocketAsyncEventArgs e)
                {
                    try
                    {
                        ProcessReceive(e, current_handle);
                    }
                    catch (Exception)
                    {

                        throw;
                    }
                }
            }
            catch (Exception ex)
            {

                throw;
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
            UserTokenSocket = null;
            token.Socket.Close(); //Socket连接关闭

            Interlocked.Decrement(ref m_numConnectedSockets); //当前Socket连接数线程安全递减

            m_readWritePool.Push(e); //Sokcet事件处理池新增

            m_maxNumberAcceptedClients.Release(); //信号量释放
            Console.WriteLine("A Client has been disconnected from the SocketServerManager. There are {0} clients connected in SocketClientManager", m_numConnectedSockets);
        }


        /// <summary>
        /// 消息发送
        /// </summary>
        /// <param name="ip_address"></param>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public async Task SendAsync(byte[] bytes)
        {
            if (UserTokenSocket == null)
            {
                Console.WriteLine("cannot found SocketAsyncEventArgs in dictionary!");
                return;
            }
            SocketAsyncEventArgs saea = m_readWritePool.Pop();
            ((AsyncUserToken)saea.UserToken).Socket = UserTokenSocket;
            saea.Completed += new EventHandler<SocketAsyncEventArgs>(IO_SendCompleted);

            saea.SetBuffer(bytes, 0, bytes.Length);
            bool willRaiseEvent = UserTokenSocket.SendAsync(saea);
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

        public void StopSocket()
        {
            UserTokenSocket.Close();
            clientSocket.Close();
        }
    }
}
