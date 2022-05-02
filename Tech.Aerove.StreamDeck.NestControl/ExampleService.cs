using Tech.Aerove.StreamDeck.Client;
using Tech.Aerove.StreamDeck.Client.Events;

namespace Tech.Aerove.StreamDeck.NestControl
{

    public class ExampleService : BackgroundService
    {
        private readonly ILogger<ExampleService> _logger;
        private readonly EventManager _eventsManager;
        private readonly IElgatoDispatcher _elgatoDispatcher;

        public ExampleService(ILogger<ExampleService> logger, EventManager eventsManager, IElgatoDispatcher dispatcher)
        {
            _logger = logger;
            _eventsManager = eventsManager;
            _elgatoDispatcher = dispatcher;
            _eventsManager.OnSendToPlugin += OnSendToPlugin;
            _eventsManager.OnDidReceiveSettings += OnDidRecieveSettings;
            _eventsManager.OnDidReceiveGlobalSettings += OnDidRecieveGlobalSettings;
        }
        protected void OnSendToPlugin(object? sender, SendToPluginEvent e)
        {

        }
        protected void OnDidRecieveSettings(object? sender, DidReceiveSettingsEvent e)
        {

        }
        protected void OnDidRecieveGlobalSettings(object? sender, DidReceiveGlobalSettingsEvent e)
        {

        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000);
            }
        }
    }
}