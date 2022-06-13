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
    [PluginAction("tech.aerove.streamdeck.nestcontrol.temperaturedown")]
    public class TemperatureDown : ActionBase
    {
        private string DeviceName => $"{Context.Settings["device"]}";

        private int TemperatureStep => int.Parse($"{Context.Settings["temperatureStep"]}");
        private decimal CurrentSetPoint = 0;

        private readonly ILogger<TemperatureDown> _logger;
        private readonly ExampleService _handler;
        public TemperatureDown(ILogger<TemperatureDown> logger, ExampleService handler)
        {
            _logger = logger;
            _handler = handler;
        }


        private ThermostatDevice Thermostat => GetThermostat();
        private ThermostatDevice _thermostat { get; set; }
        private ThermostatDevice GetThermostat()
        {
            var lookupThermostat = _handler.GetDevice(DeviceName);
            if (_thermostat == null || lookupThermostat != _thermostat)
            {
                _thermostat = lookupThermostat;
            }
            return _thermostat;
        }
        public override async Task WillAppearAsync()
        {
            await Dispatcher.SetTitleAsync($"-");
        }
        public override async Task KeyDownAsync(int userDesiredState)
        {
            if (Thermostat.Mode == ThermostatMode.HEATCOOL) { await Dispatcher.ShowAlertAsync(); return; }
            if (Thermostat.Mode == ThermostatMode.OFF)
            {
                Thermostat.SetMode(ThermostatMode.COOL);
                return;
            }
            var success = Thermostat.SetTempDown(TemperatureStep);
            if (!success) { await Dispatcher.ShowAlertAsync(); return; }
            CurrentSetPoint = Thermostat.SetPoint;
            await Dispatcher.ShowOkAsync();
        }
    }
}
