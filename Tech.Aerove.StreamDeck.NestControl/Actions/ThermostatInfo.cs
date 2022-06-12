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
        private ThermostatDevice Thermostat => _handler.GetDevice(DeviceName);
        private int TemperatureStep => int.Parse($"{Context.Settings["temperatureStep"]}");
        private decimal CurrentSetPoint = 0;

        private readonly ILogger<TemperatureDown> _logger;
        private readonly ExampleService _handler;
        public ThermostatInfo(ILogger<TemperatureDown> logger, ExampleService handler)
        {
            _logger = logger;
            _handler = handler;
            _ = Ticker();
        }
        public async Task Ticker()
        {
            while (true)
            {
                await Task.Delay(5000);
                if (string.IsNullOrWhiteSpace(DeviceName)) { continue; }
                try
                {
                    if (CurrentSetPoint != Thermostat.SetPoint)
                    {
                        CurrentSetPoint = Thermostat.SetPoint;
                    }
                    await Dispatcher.SetTitleAsync($"{Thermostat.Mode}\n{Thermostat.SetPoint}");
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
