using MMORPG.Common.Network;
using GameServer.Tool;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using GameServer.UserSystem;

namespace GameServer.Network
{
    public class NetChannel : Connection
    {
        public User? User { get; private set; }
        public long LastActiveTime { get; set; }
        public LinkedListNode<NetChannel>? LinkedListNode { get; set; }

        private string _remoteEpName;

        public NetChannel(Socket socket) : base(socket)
        {
            _remoteEpName = _socket.RemoteEndPoint?.ToString() ?? "NULL";

            ConnectionClosed += OnConnectionClosed;
            ErrorOccur += OnErrorOccur;
            WarningOccur += OnWarningOccur;
        }

        public void SetUser(User user)
        {
            User = user;
        }

        private void OnWarningOccur(object? sender, WarningOccurEventArgs e)
        {
            Log.Warning($"[Channel:{this}] A warning appears:{e.Description}");
        }

        private void OnErrorOccur(object? sender, ErrorOccurEventArgs e)
        {
            Log.Error($"[Channel:{this}] Exception occurred:{e.Exception}");
        }

        private void OnConnectionClosed(object? sender, ConnectionClosedEventArgs e)
        {
            if (e.IsManual)
            {
                Log.Information($"[Channel:{this}] Connection closed by server");
            }
            else
            {
                Log.Information($"[Channel:{this}] The peer closes the connection");
            }
        }

        public override string ToString()
        {
            var name = _remoteEpName;
            if (User != null)
            {
                name += $"({User.UserId}:{User.DbUser.Username}";
                if (User.Player != null)
                {
                    name += $":{User.Player.Name}";
                }

                name += ")";
            }

            return name;
        }
    }
}
