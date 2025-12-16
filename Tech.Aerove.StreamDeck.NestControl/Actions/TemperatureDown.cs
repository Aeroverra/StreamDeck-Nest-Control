using Aeroverra.StreamDeck.Client.Actions;
using Aeroverra.StreamDeck.NestControl.Services.Nest;
using Aeroverra.StreamDeck.NestControl.Services.Nest.Models;
using Google.Apis.SmartDeviceManagement.v1.Data;
using Newtonsoft.Json.Linq;

namespace Aeroverra.StreamDeck.NestControl.Actions
{
    [PluginActionAttribute("aeroverra.streamdeck.nestcontrol.temperaturedown")]
    public class TemperatureDown : ActionBase
    {
        private string DeviceName => $"{Context.Settings["device"]}";
        private int TemperatureStep => int.Parse($"{Context.Settings["temperatureStep"]}");
        private GoogleHomeEnterpriseSdmV1Device? Thermostat = null;

        private readonly ILogger<TemperatureDown> _logger;
        private readonly NestService _nestService;

        public TemperatureDown(ILogger<TemperatureDown> logger, NestService nestService)
        {
            _logger = logger;
            _nestService = nestService;
            nestService.OnConnected += NestService_OnConnected;
        }

        private void NestService_OnConnected(object? sender, EventArgs e)
        {
            Thermostat = _nestService.Devices
                .Where(x => x.Name == DeviceName)
                .FirstOrDefault();
        }

        public override async Task WillAppearAsync()
        {
            await Dispatcher.SetTitleAsync($"-");
            Thermostat = _nestService.Devices
                .Where(x => x.Name == DeviceName)
                .FirstOrDefault();
        }

        public override async Task DidReceiveSettingsAsync(JObject settings)
        {
            Thermostat = _nestService.Devices
                .Where(x => x.Name == DeviceName)
                .FirstOrDefault();
        }

        public override async Task KeyDownAsync(int userDesiredState)
        {
            if (Thermostat == null)
                return;

            var thermostatMode = Thermostat.GetThermostatMode();

            if (thermostatMode.Mode == ThermostatMode.OFF)
            {
                await _nestService.SetMode(Thermostat, ThermostatMode.COOL);
                return;
            }

            var setPoint = Thermostat.GetThermostatSetPoint();
            decimal heat = setPoint.HeatCelsius.ToFahrenheit();
            decimal cool = setPoint.CoolCelsius.ToFahrenheit();
            heat -= TemperatureStep;
            cool -= TemperatureStep;
            setPoint.HeatCelsius = heat.ToCelsius();
            setPoint.CoolCelsius = cool.ToCelsius();

            var success = await _nestService.SetTemp(Thermostat, setPoint.HeatCelsius, setPoint.CoolCelsius);

            if (!success) { await Dispatcher.ShowAlertAsync(); return; }
            await Dispatcher.ShowOkAsync();
        }
    }
}
