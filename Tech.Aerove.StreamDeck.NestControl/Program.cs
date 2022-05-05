using Serilog;
using Tech.Aerove.StreamDeck.Client;
using Tech.Aerove.StreamDeck.NestControl;

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
