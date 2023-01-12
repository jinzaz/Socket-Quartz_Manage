using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;
using System;
using System.Net;

namespace Socket_Quartz_Manage.SocketBase.SocketClient
{
    public interface ISocketClientManager
    {
        Task SendAsync(string ip_address, byte[] bytes);
        void Init(int numConnections, int ReceiveBufferSize, Dictionary<IPAddress, Action<byte[], SocketAsyncEventArgs>> dicreceiveInvoke);
        void AddSocketClient(string ipaddress, int port);
        void DeleteSocketClient(string ipaddress);
        IReadOnlyDictionary<IPAddress, Action<byte[], SocketAsyncEventArgs>> dic_receiveInvoke { get; set; }
    }
}
