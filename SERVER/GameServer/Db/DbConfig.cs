using System;
using System.Collections.Generic;
using System.IO;

namespace GameServer.Db
{
    public class DbConfig
    {
        public static readonly string Host;
        public static readonly int Port;
        public static readonly string User;
        public static readonly string Password;
        public static readonly string DbName;

        static DbConfig()
        {
            var config = ReadIni("Config.ini");
            Host = config.GetValueOrDefault("Host", "127.0.0.1");
            Port = int.TryParse(config.GetValueOrDefault("Port", "3306"), out var port) ? port : 3306;
            User = config.GetValueOrDefault("User", "username");
            Password = config.GetValueOrDefault("Password", "password");
            DbName = config.GetValueOrDefault("DbName", "mmorpg");
        }

        private static Dictionary<string, string> ReadIni(string path)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (!File.Exists(path))
                return dict;

            foreach (var line in File.ReadAllLines(path))
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith(";") || trimmed.StartsWith("["))
                    continue;
                var idx = trimmed.IndexOf('=');
                if (idx > 0)
                {
                    var key = trimmed.Substring(0, idx).Trim();
                    var value = trimmed.Substring(idx + 1).Trim();
                    dict[key] = value;
                }
            }
            return dict;
        }
    }
}
