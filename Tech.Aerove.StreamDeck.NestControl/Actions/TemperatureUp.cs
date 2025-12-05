using Aeroverra.StreamDeck.Client.Actions;
using Aeroverra.StreamDeck.NestControl.Services.Nest;
using Aeroverra.StreamDeck.NestControl.Services.Nest.Models;
using Google.Apis.SmartDeviceManagement.v1.Data;
using Newtonsoft.Json.Linq;

namespace Aeroverra.StreamDeck.NestControl.Actions
{

    [PluginAction("tech.aerove.streamdeck.nestcontrol.temperatureup")]
    public class TemperatureUp : ActionBase
    {
        private string DeviceName => $"{Context.Settings["device"]}";
        private int TemperatureStep => int.Parse($"{Context.Settings["temperatureStep"]}");
        private GoogleHomeEnterpriseSdmV1Device? Thermostat = null;

        private readonly NestService _nestService;
        private readonly ILogger<TemperatureUp> _logger;
        public TemperatureUp(ILogger<TemperatureUp> logger, NestService handler)
        {
            _logger = logger;
            _nestService = handler;
            _nestService.OnSetupComplete += NestService_OnSetupComplete;
        }

        private void NestService_OnSetupComplete(object? sender, EventArgs e)
        {
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

        public override async Task WillAppearAsync()
        {
            await Dispatcher.SetTitleAsync($"+");
            Thermostat = _nestService.Devices
                .Where(x => x.Name == DeviceName)
                .FirstOrDefault();
        }
        public override async Task KeyDownAsync(int userDesiredState)
        {
            if (Thermostat == null)
                return;

            var thermostatMode = Thermostat.Traits.GetTrait<ThermostatModeTrait>("sdm.devices.traits.ThermostatMode");

            if (thermostatMode.Mode == ThermostatMode.OFF)
            {
                _nestService.SetMode(Thermostat, ThermostatMode.HEAT);
                return;
            }

            var setPoint = Thermostat.Traits.GetTrait<ThermostatSetpointTrait>("sdm.devices.traits.ThermostatTemperatureSetpoint");
            decimal heat = setPoint.HeatCelsius.ToFahrenheit();
            decimal cool = setPoint.CoolCelsius.ToFahrenheit();
            heat += TemperatureStep;
            cool += TemperatureStep;
            setPoint.HeatCelsius = heat.ToCelsius();
            setPoint.CoolCelsius = cool.ToCelsius();

            var success = _nestService.SetTemp(Thermostat, setPoint.HeatCelsius, setPoint.CoolCelsius);

            if (!success) { await Dispatcher.ShowAlertAsync(); return; }
            await Dispatcher.ShowOkAsync();
        }
    }
}
