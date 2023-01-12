using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Socket_Quartz_Manage.SocketBase.SocketService
{
    public interface ISocketServerManager
    {
        Task SendAsync(string ip_address, byte[] bytes);
        void Init(int numConnections, int ReceiveBufferSize, Dictionary<IPAddress, Action<byte[], SocketAsyncEventArgs>> dicreceiveInvoke);
        void start(IPEndPoint localEndPoint);

        IReadOnlyDictionary<IPAddress, Action<byte[], SocketAsyncEventArgs>> dic_receiveInvoke { get; set; }
    }
}
