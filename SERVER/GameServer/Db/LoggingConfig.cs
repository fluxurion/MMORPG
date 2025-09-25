using System;
using Serilog;
using Serilog.Events;
using GameServer.Config;

namespace GameServer.Db
{
    public static class LoggingConfig
    {
        public static string Level { get; private set; } = "Information";
        public static string LogFile { get; private set; } = "Logs/log-.txt";
        public static string DBErrorFile { get; private set; } = "DBErrors.log";

        static LoggingConfig()
        {
            var config = AppConfig.ReadIni("Config.ini");
            Level = config.GetValueOrDefault("Level", Level);
            LogFile = config.GetValueOrDefault("LogFile", LogFile);
            DBErrorFile = config.GetValueOrDefault("DBErrorFile", DBErrorFile);
        }

        public static void LogDbError(Exception ex, string message)
        {
            // Use a static logger instance for DB errors to avoid creating a new logger each time
            // (or create a new one if you prefer, but this is more efficient)
            var dbLogger = new LoggerConfiguration()
                .WriteTo.File(DBErrorFile, rollingInterval: RollingInterval.Day)
                .CreateLogger();

            dbLogger.Error(ex, message);
            // Optionally dispose if you want, but Serilog handles this well for static loggers
        }
    }
}