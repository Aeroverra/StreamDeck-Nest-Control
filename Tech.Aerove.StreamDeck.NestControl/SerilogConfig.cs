using Serilog;

namespace Tech.Aerove.StreamDeck.NestControl
{
    internal class SerilogConfig
    {
        public static void Configure()
        {
            string ConsoleFormat = "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext:l}] {Message:lj}{NewLine}{Exception}";
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 4)
                .CreateLogger();
        }
    }
}
