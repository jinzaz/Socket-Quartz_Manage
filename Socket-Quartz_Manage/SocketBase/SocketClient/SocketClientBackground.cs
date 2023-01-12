using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Socket_Quartz_Manage.Config;
using Microsoft.Extensions.Options;
using Socket_Quartz_Manage.Extensions;

namespace Socket_Quartz_Manage.SocketBase.SocketClient
{
    public class SocketClientBackground : BackgroundService, ISocketClientManager
    {
        private int m_numConnections; //客户端最大连接数
        private int m_receiveBufferSize; //单个消息接收数据大小
        BufferManager m_bufferManager;
        const int opsToPreAlloc = 2;

        SocketAsyncEventArgsPool m_readWritePool;
        int m_totalBytesRead; //当前服务器端读取数据总数
        int m_numConnectedSockets; //当前连接的客户端Socket数
        Semaphore m_maxNumberAcceptedClients;
        public IReadOnlyDictionary<IPAddress, Action<byte[], SocketAsyncEventArgs>> dic_receiveInvoke { get; set; } //储存客户端对应的接收消息处理方法委托

        private readonly AppSettingsConfig _appSettingsConfig;

        Dictionary<string, SocketClient> dic_ConnectedSockets;

        public SocketClientBackground(IOptions<AppSettingsConfig> options)
        {
            _appSettingsConfig = options.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Init(30, 1024, Get_dic_SocketActions());


            string ipaddress = "188.128.0.140";
            int port = 8522;
            SocketClient client = new SocketClient(m_readWritePool, m_maxNumberAcceptedClients, ref m_totalBytesRead, ref m_numConnectedSockets);
            IPEndPoint iPEndPoint = new IPEndPoint(IPAddress.Parse(ipaddress), port);
            client.start(iPEndPoint, dic_receiveInvoke[IPAddress.Parse(ipaddress)]);
            dic_ConnectedSockets.Add(ipaddress, client);
            
            await Task.CompletedTask;
        }


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
            dic_ConnectedSockets = new Dictionary<string, SocketClient>();

            m_bufferManager.InitBuffer();
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


        public void AddSocketClient(string ipaddress, int port)
        {
            SocketClient client = new SocketClient(m_readWritePool, m_maxNumberAcceptedClients, ref m_totalBytesRead, ref m_numConnectedSockets);
            IPEndPoint iPEndPoint = new IPEndPoint(IPAddress.Parse(ipaddress), port);
            client.start(iPEndPoint, dic_receiveInvoke[IPAddress.Parse(ipaddress)]);
            dic_ConnectedSockets.Add(ipaddress, client);
        }

        public void DeleteSocketClient(string ipaddress)
        {
            var socket = dic_ConnectedSockets[ipaddress];
            socket.StopSocket();
            dic_ConnectedSockets.Remove(ipaddress);
            Console.WriteLine("m_readWritePool" + m_readWritePool.Count);
            Console.WriteLine("m_numConnectedSockets" + m_numConnectedSockets);
        }

        public async Task SendAsync(string ip_address, byte[] bytes)
        {
            if (!dic_ConnectedSockets.ContainsKey(ip_address))
            {
                Console.WriteLine("cannot found SocketInstance in dictionary!");
                return;
            }
            var socket = dic_ConnectedSockets[ip_address];
            await socket.SendAsync(bytes);
        }


        /// <summary>
        /// 获取设备消息处理方法列表
        /// </summary>
        /// <returns></returns>
        private Dictionary<IPAddress, Action<byte[], SocketAsyncEventArgs>> Get_dic_SocketActions()
        {
            Dictionary<IPAddress, Action<byte[], SocketAsyncEventArgs>> dic_SocketActions = new Dictionary<IPAddress, Action<byte[], SocketAsyncEventArgs>>();


            dic_SocketActions.Add(IPAddress.Parse("188.128.0.140"), (bytes, saea) =>
            {
                AsyncUserToken token = saea.UserToken as AsyncUserToken;
                var str = bytes.ToHexStrFromByte();
                var ip_address = ((IPEndPoint)token.Socket.RemoteEndPoint).Address.ToString();
                Console.WriteLine(ip_address + " Receive：" + str);

            });
            return dic_SocketActions;
        }


    }
}
