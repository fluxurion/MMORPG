using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Db
{
    public class DbConfig
    {
        /// <summary>
        /// DatabaseHost
        /// </summary>
        public static readonly string Host = "127.0.0.1";
        /// <summary>
        /// Database port
        /// </summary>
        public static readonly int Port = 3306;
        /// <summary>
        /// Database username
        /// </summary>
        public static readonly string User = "root";
        /// <summary>
        /// Database password
        /// </summary>
        public static readonly string Password = "root";
        /// <summary>
        /// Database name
        /// </summary>
        public static readonly string DbName = "MMORPG";
    }
}
