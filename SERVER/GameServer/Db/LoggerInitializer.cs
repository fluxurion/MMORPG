using Serilog;
using Serilog.Events;

namespace GameServer.Db
{
    public static class LoggerInitializer
    {
        public static void ConfigureLogger()
        {
            var level = LoggingConfig.Level switch
            {
                "Debug" => LogEventLevel.Debug,
                "Information" => LogEventLevel.Information,
                "Warning" => LogEventLevel.Warning,
                "Error" => LogEventLevel.Error,
                "Fatal" => LogEventLevel.Fatal,
                _ => LogEventLevel.Information
            };

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Is(level)
                .WriteTo.Console()
                .WriteTo.File(LoggingConfig.LogFile, rollingInterval: RollingInterval.Day)
                .CreateLogger();
        }
    }
}