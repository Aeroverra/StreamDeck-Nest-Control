using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tech.Aerove.StreamDeck.Client.Actions;
using Tech.Aerove.StreamDeck.NestControl.Tech.Aerove.Tools.Nest;
using Tech.Aerove.Tools.Nest;
using Tech.Aerove.Tools.Nest.Models;

namespace Tech.Aerove.StreamDeck.NestControl.Actions
{
    [PluginAction("tech.aerove.streamdeck.nestcontrol.thermostatinfo")]
    public class ThermostatInfo : ActionBase
    {
        private string DeviceName => $"{Context.Settings["device"]}";

        private ThermostatDevice Thermostat { get; set; }

        private readonly ILogger<TemperatureDown> _logger;
        private readonly ExampleService _handler;
        public ThermostatInfo(ILogger<TemperatureDown> logger, ExampleService handler)
        {
            _logger = logger;
            _handler = handler;
            _ = AwaitDevice();
            _ = Ticker();
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
                    Thermostat = lookupThermostat;
                    return;
                }
                catch
                {
                    await Task.Delay(2000);
                }
            }
        }

        public async Task Ticker()
        {
            while (true)
            {
                await Task.Delay(5000);
                if (string.IsNullOrWhiteSpace(DeviceName) || Thermostat == null) { continue; }
                try
                {
                    var mode = Thermostat.Mode;
                    var modeText = $"";
                    if (Thermostat.Mode == ThermostatMode.COOL)
                    {
                        await Dispatcher.SetImageAsync(ImageColors.Blue.DataUri);
                        modeText = $"{mode}\n{Thermostat.SetPoint}";
                    }
                    if (Thermostat.Mode == ThermostatMode.HEAT)
                    {
                        await Dispatcher.SetImageAsync(ImageColors.Red.DataUri);
                        modeText = $"{mode}\n{Thermostat.SetPoint}";
                    }
                    if (Thermostat.Mode == ThermostatMode.HEATCOOL)
                    {
                        await Dispatcher.SetImageAsync(ImageColors.RedBlue.DataUri);
                        modeText = "H&C\n";
                    }
                    if (Thermostat.Mode == ThermostatMode.OFF)
                    {
                        await Dispatcher.SetImageAsync("");
                        modeText = $"{mode}";
                    }
                    await Dispatcher.SetTitleAsync($"{modeText}");
                    await Task.Delay(5000);
                    await Dispatcher.SetTitleAsync($"{Thermostat.CurrentTemperature}");
                }
                catch (Exception)
                {

                }
            }
        }

    }
}
