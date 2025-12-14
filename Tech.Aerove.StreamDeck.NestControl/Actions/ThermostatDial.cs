using Aeroverra.StreamDeck.Client.Actions;
using Aeroverra.StreamDeck.Client.Events.SharedModels;
using Aeroverra.StreamDeck.NestControl.Services.Nest;
using Aeroverra.StreamDeck.NestControl.Services.Nest.Models;
using Google.Apis.SmartDeviceManagement.v1.Data;
using Newtonsoft.Json.Linq;

namespace Aeroverra.StreamDeck.NestControl.Actions
{
    [PluginAction("tech.aerove.streamdeck.nestcontrol.thermostatdial")]
    public class ThermostatDial : ActionBase
    {
        private string DeviceName => $"{Context.Settings["device"]}";
        private int MsDelay
        {
            get
            {
                int.TryParse($"{Context.Settings["delay"]}", out int val);
                if (val < 1000)
                {
                    val = 5000;
                }
                return val;
            }
        }


        private GoogleHomeEnterpriseSdmV1Device? Thermostat { get; set; }

        private readonly ILogger<TemperatureDown> _logger;
        private readonly NestService _nestService;
        public ThermostatDial(ILogger<TemperatureDown> logger, NestService nestService)
        {
            _logger = logger;
            _nestService = nestService;
            _ = Ticker();
            nestService.OnDeviceUpdated += NestService_OnDeviceUpdated;
            nestService.OnConnected += NestService_OnConnected;
        }

        public override async Task WillAppearAsync()
        {
            await Dispatcher.SetTitleAsync($"Aero");
            Thermostat = _nestService.Devices
                .Where(x => x.Name == DeviceName)
                .FirstOrDefault();
        }

        public override async Task DialRotateAsync(DialRotatePayload payload)
        {
            if (Thermostat == null)
                return;

            var thermostatMode = Thermostat.GetThermostatMode();
            if (thermostatMode.Mode == ThermostatMode.OFF)
            {
                if (payload.Ticks > 0)
                {
                    _nestService.SetMode(Thermostat, ThermostatMode.HEAT);
                }
                else
                {
                    _nestService.SetMode(Thermostat, ThermostatMode.COOL);
                }
            }
            bool success = false;
            var setPoint = Thermostat.GetThermostatSetPoint();
            decimal heat = setPoint.HeatCelsius.ToFahrenheit();
            decimal cool = setPoint.CoolCelsius.ToFahrenheit();

            if (payload.Ticks > 0)
            {
                heat += payload.Ticks;
                cool += payload.Ticks;
            }
            else
            {
                heat -= payload.Ticks;
                cool -= payload.Ticks;
            }

            setPoint.HeatCelsius = heat.ToCelsius();
            setPoint.CoolCelsius = cool.ToCelsius();
            success = _nestService.SetTemp(Thermostat, setPoint.HeatCelsius, setPoint.CoolCelsius);


            if (!success)
            {
                await Dispatcher.ShowAlertAsync();
            }
            else
            {
                var setPointRender = SetPointRender(thermostatMode.Mode, setPoint);
                await Dispatcher.ShowOkAsync();
                await Dispatcher.SetFeedbackAsync(new
                {
                    value = $"{setPointRender}"
                });

            }
        }

        public override async Task DialUpAsync(EncoderPayload payload)
        {
            if (Thermostat == null)
                return;

            var thermostatMode = Thermostat.GetThermostatMode();
            if (thermostatMode.Mode != ThermostatMode.OFF)
            {
                _nestService.SetMode(Thermostat, ThermostatMode.OFF);
            }
        }

        private void NestService_OnConnected(object? sender, EventArgs e)
        {
            Thermostat = _nestService.Devices
                .Where(x => x.Name == DeviceName)
                .FirstOrDefault();
        }

        private void NestService_OnDeviceUpdated(object? sender, Google.Apis.SmartDeviceManagement.v1.Data.GoogleHomeEnterpriseSdmV1Device e)
        {
            if (e.Name == DeviceName)
            {
                Thermostat = e;
            }
        }

        public override Task DidReceiveSettingsAsync(JObject settings)
        {
            Thermostat = _nestService.Devices
                .Where(x => x.Name == DeviceName)
                .FirstOrDefault();

            return Task.CompletedTask;
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


        public async Task Ticker()
        {
            var delay = 5000;
            while (true)
            {

                await Task.Delay(delay);
                if (string.IsNullOrWhiteSpace(DeviceName) || Thermostat == null)
                    continue;

                try
                {
                    var thermostatMode = Thermostat.GetThermostatMode();
                    var temp = Thermostat.GetThermostatTemperature();
                    var setPoint = Thermostat.GetThermostatSetPoint();

                    var setPointRender = SetPointRender(thermostatMode.Mode, setPoint);

                    var modeText = $"";
                    if (thermostatMode.Mode == ThermostatMode.COOL)
                    {
                        await Dispatcher.SetStateAsync(1);
                        await Dispatcher.SetImageAsync(ImageColors.Blue.DataUri);
                        modeText = $"{thermostatMode.Mode}\n{setPointRender}";
                    }
                    if (thermostatMode.Mode == ThermostatMode.HEAT)
                    {
                        await Dispatcher.SetStateAsync(1);
                        await Dispatcher.SetImageAsync(ImageColors.Red.DataUri);
                        modeText = $"{thermostatMode.Mode}\n{setPointRender}";
                    }
                    if (thermostatMode.Mode == ThermostatMode.HEATCOOL)
                    {
                        await Dispatcher.SetStateAsync(1);
                        await Dispatcher.SetImageAsync(ImageColors.RedBlue.DataUri);
                        modeText = $"H&C\n{setPointRender}";
                    }
                    if (thermostatMode.Mode == ThermostatMode.OFF)
                    {
                        await Dispatcher.SetStateAsync(0);
                        await Dispatcher.SetImageAsync("");
                        modeText = $"{thermostatMode.Mode}";
                    }
                    await Dispatcher.SetTitleAsync($"{modeText}");
                    await Dispatcher.SetFeedbackAsync(new
                    {
                        value = $"{modeText}"
                    });
                    delay = MsDelay;
                    await Task.Delay(delay);
                    await Dispatcher.SetTitleAsync($"{temp.AmbientTemperatureCelsius.ToFahrenheit().ToString("F0")}");
                    await Dispatcher.SetFeedbackAsync(new
                    {
                        value = $"{temp.AmbientTemperatureCelsius.ToFahrenheit().ToString("F0")}"
                    });
                }
                catch (Exception)
                {

                }
            }
        }

    }
}
