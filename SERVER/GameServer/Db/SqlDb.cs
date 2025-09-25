using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Serilog;

namespace GameServer.Db
{
    public class SqlDb
    {
        public static IFreeSql FreeSql { get; }

        static SqlDb()
        {
            FreeSql = new FreeSql.FreeSqlBuilder()
                .UseConnectionString(global::FreeSql.DataType.MySql, $"Data Source={DbConfig.Host};Port={DbConfig.Port};User Id={DbConfig.User};Password={DbConfig.Password};")
                .UseAutoSyncStructure(true)
            .Build();

            // Check if the database exists
            var exists = FreeSql.Ado.QuerySingle<int>($"SELECT COUNT(*) FROM information_schema.schemata WHERE schema_name = '{DbConfig.DbName}'") > 0;
            if (!exists)
            {
                FreeSql.Ado.ExecuteNonQuery($"CREATE DATABASE {DbConfig.DbName}");
                Log.Information($"The database \"{DbConfig.DbName}\" does not exist and has been automatically created");
            }

            // Relink
            FreeSql = new FreeSql.FreeSqlBuilder()
                .UseConnectionString(global::FreeSql.DataType.MySql,  $"Data Source={DbConfig.Host};Port={DbConfig.Port};User Id={DbConfig.User};Password={DbConfig.Password};" +
                                                                      $"Initial Catalog={DbConfig.DbName};Charset=utf8;SslMode=none;Max pool size=10")
                .UseAutoSyncStructure(true)
                .Build();

            exists = FreeSql.DbFirst.GetTablesByDatabase(DbConfig.DbName).Exists(t => t.Name == "user");

            if (!exists)
            {
                // FreeSql.CodeFirst.SyncStructure<DbUser>();
                FreeSql.Insert(new DbUser("root", "1234567890", Authoritys.Administrator)).ExecuteAffrows();
                Log.Information($"The \"user\" table in the database \"{DbConfig.DbName}\" does not exist. It has been automatically created and an administrator account has been added (account=root, password=1234567890)");
            }
        }
    }
}
