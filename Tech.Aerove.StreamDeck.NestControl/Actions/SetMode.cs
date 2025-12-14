using Aeroverra.StreamDeck.Client.Actions;
using Aeroverra.StreamDeck.NestControl.Services.Nest;
using Aeroverra.StreamDeck.NestControl.Services.Nest.Models;
using Google.Apis.SmartDeviceManagement.v1.Data;
using Newtonsoft.Json.Linq;

namespace Aeroverra.StreamDeck.NestControl.Actions
{

    [PluginAction("tech.aerove.streamdeck.nestcontrol.setmode")]
    public class SetMode : ActionBase
    {
        private string DeviceName => $"{Context.Settings["device"]}";
        private ThermostatMode ButtonMode => (ThermostatMode)Enum.Parse(typeof(ThermostatMode), $"{Context.Settings["setMode"]}");
        private GoogleHomeEnterpriseSdmV1Device? Thermostat { get; set; } = null;

        private readonly NestService _nestService;
        private readonly ILogger<TemperatureUp> _logger;
        public SetMode(ILogger<TemperatureUp> logger, NestService nestService)
        {
            _logger = logger;
            _nestService = nestService;
            nestService.OnConnected += NestService_OnConnected;
            nestService.OnDeviceUpdated  += NestService_OnDeviceUpdated;
        }

        private void NestService_OnDeviceUpdated(object? sender, GoogleHomeEnterpriseSdmV1Device e)
        {
            if (e.Name == DeviceName)
            {
                Thermostat = e;
                _= SetInfo();
            }
        }

        private void NestService_OnConnected(object? sender, EventArgs e)
        {
            Thermostat = _nestService.Devices
                .Where(x => x.Name == DeviceName)
                .FirstOrDefault();

            _= SetInfo();
        }

        public override async Task WillAppearAsync()
        {
            await Dispatcher.SetTitleAsync($"Aero");
            Thermostat = _nestService.Devices
                .Where(x => x.Name == DeviceName)
                .FirstOrDefault();
        }

        public override async Task DidReceiveSettingsAsync(JObject settings)
        {
            Thermostat = _nestService.Devices
                .Where(x => x.Name == DeviceName)
                .FirstOrDefault();

            await SetInfo();
        }

        public async Task SetInfo()
        {
            if (Thermostat == null)
                return;

            var thermostatMode = Thermostat.GetThermostatMode();
            var setPoint = Thermostat.GetThermostatSetPoint();

            var setPointRender = SetPointRender(thermostatMode.Mode, setPoint);
            if (thermostatMode.Mode != ButtonMode)
            {
                await Dispatcher.SetStateAsync(0);
                await Dispatcher.SetImageAsync("");
                if (ButtonMode == ThermostatMode.HEATCOOL)
                {
                    await Dispatcher.SetTitleAsync($"H&C");
                    return;
                }
                await Dispatcher.SetTitleAsync($"{ButtonMode}");
                return;
            }
            await Dispatcher.SetStateAsync(1);
            if (thermostatMode.Mode == ThermostatMode.COOL)
            {
                await Dispatcher.SetImageAsync(ImageColors.Blue.DataUri);
                await Dispatcher.SetTitleAsync($"{thermostatMode.Mode}\n{setPointRender}");
            }
            else if (thermostatMode.Mode == ThermostatMode.HEAT)
            {
                await Dispatcher.SetImageAsync(ImageColors.Red.DataUri);
                await Dispatcher.SetTitleAsync($"{thermostatMode.Mode}\n{setPointRender}");
            }
            else if (thermostatMode.Mode == ThermostatMode.HEATCOOL)
            {
                await Dispatcher.SetImageAsync(ImageColors.RedBlue.DataUri);
                await Dispatcher.SetTitleAsync($"H&C");
            }
            else if (thermostatMode.Mode == ThermostatMode.OFF)
            {
                await Dispatcher.SetImageAsync("");
                await Dispatcher.SetTitleAsync($"{thermostatMode.Mode}\nNone\nSet");
            }


        }

        public string SetPointRender(ThermostatMode mode, ThermostatSetpointTrait setPoint)
        {
            if (mode == ThermostatMode.COOL)
            {
                return setPoint.CoolCelsius.ToFahrenheit().ToString("F0");
            }
            else if (mode == ThermostatMode.HEAT)
            {
                return setPoint.HeatCelsius.ToFahrenheit().ToString("F0");
            }
            else if (mode == ThermostatMode.HEATCOOL)
            {
                return $"{setPoint.CoolCelsius.ToFahrenheit().ToString("F0")}-{setPoint.HeatCelsius.ToFahrenheit().ToString("F0")}";
            }
            return "Err";
        }

        public override async Task KeyDownAsync(int userDesiredState)
        {
            if (Thermostat == null)
                return;

            var thermostatMode = Thermostat.GetThermostatMode();

            var success = false;
            if (thermostatMode.Mode != ThermostatMode.OFF)
            {
                _nestService.SetMode(Thermostat, ThermostatMode.OFF);
            }
            else
            {
                _nestService.SetMode(Thermostat, ButtonMode);
            }
            if (!success)
            {
                await Dispatcher.ShowAlertAsync(); return;
            }
            await SetInfo();
        }
    }
}
