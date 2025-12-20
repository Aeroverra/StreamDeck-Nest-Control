using Aeroverra.StreamDeck.Client;
using Aeroverra.StreamDeck.Client.Services;
using Aeroverra.StreamDeck.NestControl;
using Aeroverra.StreamDeck.NestControl.Services;
using Aeroverra.StreamDeck.NestControl.Services.Nest;
using Serilog;

public partial class Program
{
    private static async Task Main(string[] args)
    {
        //https://drive.google.com/file/d/1Jv505KUZI2UDplwoDLRlLMQGCt_mTl0m/view
        //https://developers.google.com/nest/device-access/get-started
        //20777
        SerilogConfig.Configure();

        IHost host = Host.CreateDefaultBuilder(args)
            .UseSerilog()
            .ConfigureServices((context, services) =>
            {
                services.AddAeroveStreamDeckClient(context);
                services.AddHostedService<CoreWorker>();
                services.AddSingleton<GlobalSettings>();
                services.AddSingleton<IGlobalSettings>(x => x.GetRequiredService<GlobalSettings>());
                services.AddSingleton<NestService>();
                services.AddSingleton<CoreWorker>();
                services.AddSingleton<DisplayService>();
            })
            .Build();

        await host.RunAsync();
    }
}