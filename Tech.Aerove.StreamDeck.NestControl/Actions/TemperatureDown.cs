using Aeroverra.StreamDeck.Client.Actions;
using Tech.Aerove.Tools.Nest.Models;

namespace Tech.Aerove.StreamDeck.NestControl.Actions
{
    [PluginAction("tech.aerove.streamdeck.nestcontrol.temperaturedown")]
    public class TemperatureDown : ActionBase
    {
        private string DeviceName => $"{Context.Settings["device"]}";
        private int TemperatureStep => int.Parse($"{Context.Settings["temperatureStep"]}");
        private ThermostatDevice Thermostat => _handler.GetDevice(DeviceName);

        private readonly ILogger<TemperatureDown> _logger;
        private readonly ExampleService _handler;
        public TemperatureDown(ILogger<TemperatureDown> logger, ExampleService handler)
        {
            _logger = logger;
            _handler = handler;
        }
        public override async Task WillAppearAsync()
        {
            await Dispatcher.SetTitleAsync($"-");
        }
        public override async Task KeyDownAsync(int userDesiredState)
        {
            if (Thermostat.Mode == ThermostatMode.OFF)
            {
                Thermostat.SetMode(ThermostatMode.COOL);
                return;
            }
            var success = Thermostat.SetTempDown(TemperatureStep);
            if (!success) { await Dispatcher.ShowAlertAsync(); return; }
            await Dispatcher.ShowOkAsync();
        }
    }
}
