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

    [PluginAction("tech.aerove.streamdeck.nestcontrol.temperatureup")]
    public class TemperatureUp : ActionBase
    {
        private string DeviceName => $"{Context.Settings["device"]}";
        private int TemperatureStep => int.Parse($"{Context.Settings["temperatureStep"]}");
        private ThermostatDevice Thermostat => _handler.GetDevice(DeviceName);

        private readonly ExampleService _handler;
        private readonly ILogger<TemperatureUp> _logger;
        public TemperatureUp(ILogger<TemperatureUp> logger, ExampleService handler)
        {
            _logger = logger;
            _handler = handler;
        }

      

        public override async Task WillAppearAsync()
        {
            await Dispatcher.SetTitleAsync($"+");
        }
        public override async Task KeyDownAsync(int userDesiredState)
        {
            if (Thermostat.Mode == ThermostatMode.OFF)
            {
                Thermostat.SetMode(ThermostatMode.HEAT);
                return;
            }
            var success = Thermostat.SetTempUp(TemperatureStep);
            if (!success) { await Dispatcher.ShowAlertAsync(); return; }
            await Dispatcher.ShowOkAsync();
        }
    }
}
