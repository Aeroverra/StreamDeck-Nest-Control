using Aeroverra.StreamDeck.Client.Actions;
using Aeroverra.StreamDeck.NestControl.Services.Nest;
using Aeroverra.StreamDeck.NestControl.Services.Nest.Models;
using Google.Apis.SmartDeviceManagement.v1.Data;
using Newtonsoft.Json.Linq;

namespace Aeroverra.StreamDeck.NestControl.Actions
{

    [PluginActionAttribute("aeroverra.streamdeck.nestcontrol.setmode")]
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

        public override Task OnInitializedAsync()
        {
            return Dispatcher.SetTitleAsync($"Aero");
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
            Thermostat = _nestService.Devices
                .Where(x => x.Name == DeviceName)
                .FirstOrDefault();

            await SetInfo();
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
                await Dispatcher.SetTitleAsync($"{thermostatMode.Mode}");
            }
            else if (thermostatMode.Mode == ThermostatMode.HEAT)
            {
                await Dispatcher.SetImageAsync(ImageColors.Red.DataUri);
                await Dispatcher.SetTitleAsync($"{thermostatMode.Mode}");
            }
            else if (thermostatMode.Mode == ThermostatMode.HEATCOOL)
            {
                await Dispatcher.SetImageAsync(ImageColors.RedBlue.DataUri);
                await Dispatcher.SetTitleAsync($"H&C");
            }
            else if (thermostatMode.Mode == ThermostatMode.OFF)
            {
                await Dispatcher.SetImageAsync("");
                await Dispatcher.SetTitleAsync($"{thermostatMode.Mode}");
            }


        }

        public override async Task KeyDownAsync(int userDesiredState)
        {
            if (Thermostat == null)
                return;

            var thermostatMode = Thermostat.GetThermostatMode();

            var success = false;
            if (thermostatMode.Mode == ButtonMode)
            {
                success = await _nestService.SetMode(Thermostat, ThermostatMode.OFF);
            }
            else
            {
                success = await _nestService.SetMode(Thermostat, ButtonMode);
            }
            if (!success)
            {
                await Dispatcher.ShowAlertAsync(); return;
            }
            await SetInfo();
        }
    }
}
