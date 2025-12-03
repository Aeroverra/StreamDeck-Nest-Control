using Serilog;
using Aeroverra.StreamDeck.Client;
using Tech.Aerove.StreamDeck.NestControl;

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
                services.AddHostedService((sp) => sp.GetRequiredService<ExampleService>());
                services.AddSingleton<ExampleService>();
            })
            .Build();

        await host.RunAsync();
    }
}
//Welcome to spaghetti land. I normally don't allow my code quality to get this bad but this was originally 
//a test bench for my sdk and released it as is because it was stable. Eventually I will get to cleaning it up :P