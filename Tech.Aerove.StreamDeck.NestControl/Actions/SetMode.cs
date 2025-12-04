using Aeroverra.StreamDeck.Client.Actions;
using Newtonsoft.Json.Linq;
using Tech.Aerove.Tools.Nest.Models;

namespace Aeroverra.StreamDeck.NestControl.Actions
{

    [PluginAction("tech.aerove.streamdeck.nestcontrol.setmode")]
    public class SetMode : ActionBase
    {
        private string DeviceName => $"{Context.Settings["device"]}";
        private ThermostatMode ButtonMode => (ThermostatMode)Enum.Parse(typeof(ThermostatMode), $"{Context.Settings["setMode"]}");
        private ThermostatDevice Thermostat { get; set; }

        private readonly ExampleService _handler;
        private readonly ILogger<TemperatureUp> _logger;
        public SetMode(ILogger<TemperatureUp> logger, ExampleService handler)
        {
            _logger = logger;
            _handler = handler;
            _ = AwaitDevice();
        }
        public override async Task DidReceiveSettingsAsync(JObject settings)
        {
            await SetInfo();
        }
        private async Task AwaitDevice()
        {
            while (true)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(DeviceName))
                    {
                        await Task.Delay(2000);
                        continue;
                    }
                    var lookupThermostat = _handler.GetDevice(DeviceName);
                    if (Thermostat != null)
                    {
                        Thermostat.OnUpdate -= OnUpdate;
                    }
                    Thermostat = lookupThermostat;
                    Thermostat.OnUpdate += OnUpdate;
                    await SetInfo();
                    return;
                }
                catch
                {
                    await Task.Delay(2000);
                }
            }
        }

        public void OnUpdate()
        {
            _ = SetInfo();
        }
        public async Task SetInfo()
        {
            if (Thermostat.Mode != ButtonMode)
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
            if (Thermostat.Mode == ThermostatMode.COOL)
            {
                await Dispatcher.SetImageAsync(ImageColors.Blue.DataUri);
                await Dispatcher.SetTitleAsync($"{Thermostat.Mode}\n{Thermostat.SetPointRender}");
            }
            else if (Thermostat.Mode == ThermostatMode.HEAT)
            {
                await Dispatcher.SetImageAsync(ImageColors.Red.DataUri);
                await Dispatcher.SetTitleAsync($"{Thermostat.Mode}\n{Thermostat.SetPointRender}");
            }
            else if (Thermostat.Mode == ThermostatMode.HEATCOOL)
            {
                await Dispatcher.SetImageAsync(ImageColors.RedBlue.DataUri);
                await Dispatcher.SetTitleAsync($"H&C");
            }
            else if (Thermostat.Mode == ThermostatMode.OFF)
            {
                await Dispatcher.SetImageAsync("");
                await Dispatcher.SetTitleAsync($"{Thermostat.Mode}\nNone\nSet");
            }


        }
        public override async Task KeyDownAsync(int userDesiredState)
        {
            var success = false;
            if (Thermostat.Mode != ThermostatMode.OFF)
            {
                success = Thermostat.SetMode(ThermostatMode.OFF);
            }
            else
            {
                success = Thermostat.SetMode(ButtonMode);
            }
            if (!success)
            {
                await Dispatcher.ShowAlertAsync(); return;
            }
            await SetInfo();
        }
    }
}
