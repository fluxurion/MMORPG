using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MMORPG.Common.Network
{
    public static class NetConfig
    {
        /// <summary>
        /// Packet header size
        /// </summary>
        public static readonly int PacketHeaderSize = 8;
        /// <summary>
        /// Maximum packet size
        /// </summary>
        public static readonly int MaxPacketSize = 1024 * 64;
        /// <summary>
        /// Server port
        /// </summary>
        public static readonly int ServerPort = 11451;


        /// <summary>
        /// Server IP address
        /// </summary>
        public static readonly IPAddress ServerIpAddress = IPAddress.Parse("127.0.0.1");
    }
}
