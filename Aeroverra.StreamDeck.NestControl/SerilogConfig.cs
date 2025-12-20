using Serilog;

namespace Aeroverra.StreamDeck.NestControl
{
    internal class SerilogConfig
    {
        public static void Configure()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 4)
                .CreateLogger();
        }
    }
}
