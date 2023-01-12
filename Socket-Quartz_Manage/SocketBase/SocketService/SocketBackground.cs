using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Socket_Quartz_Manage.SocketBase;
using Socket_Quartz_Manage.Config;
using Socket_Quartz_Manage.Extensions;

namespace Socket_Quartz_Manage.SocketBase.SocketService
{
    public class SocketBackground : BackgroundService
    {
        private string BindIpAddress;
        private int BindPort;

        private readonly ISocketServerManager _socketServerManager;
        private readonly AppSettingsConfig _appSettingsConfig;
        public SocketBackground(ISocketServerManager socketServerManager, IOptions<AppSettingsConfig> options)
        {
            _socketServerManager = socketServerManager;
            _appSettingsConfig = options.Value;
            BindIpAddress = _appSettingsConfig.SocketBindIP;
            BindPort = _appSettingsConfig.SocketBindPort;
        }

        /// <summary>
        /// Socket服务器端启动
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Dictionary<IPAddress, Action<byte[], SocketAsyncEventArgs>> dic = Get_dic_SocketActions();
            _socketServerManager.Init(30, 1024, dic);

            IPAddress iPAddress = string.IsNullOrEmpty(BindIpAddress) ? IPAddress.Any : IPAddress.Parse(BindIpAddress);
            IPEndPoint iPEndPoint = new IPEndPoint(iPAddress, BindPort);
            _socketServerManager.start(iPEndPoint);
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
                Console.WriteLine(((IPEndPoint)token.Socket.RemoteEndPoint).Address.ToString() + " Receive：" + str);

            });



            
            dic_SocketActions.Add(IPAddress.Parse("188.128.0.244"), (bytes, saea) =>
            {
                AsyncUserToken token = saea.UserToken as AsyncUserToken;
                var str = bytes.ToHexStrFromByte();
                Console.WriteLine(((IPEndPoint)token.Socket.RemoteEndPoint).Address.ToString() + " Receive：" + str);

            });

            return dic_SocketActions;
        }

    }
}
