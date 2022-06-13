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

    [PluginAction("tech.aerove.streamdeck.nestcontrol.setmode")]
    public class SetMode : ActionBase
    {
        private string DeviceName => $"{Context.Settings["device"]}";

        private int TemperatureStep => int.Parse($"{Context.Settings["temperatureStep"]}");
        private ThermostatMode CurrentMode = ThermostatMode.OFF;

        private readonly ExampleService _handler;
        private readonly ILogger<TemperatureUp> _logger;
        public SetMode(ILogger<TemperatureUp> logger, ExampleService handler)
        {
            _logger = logger;
            _handler = handler;
            _ = OnUpdateAsync();
        }
        private ThermostatDevice Thermostat => GetThermostat();
        private ThermostatDevice _thermostat { get; set; }
        private ThermostatDevice GetThermostat()
        {
            var lookupThermostat = _handler.GetDevice(DeviceName);
            if (_thermostat == null || lookupThermostat.Name != _thermostat.Name)
            {
                if (_thermostat != null)
                {
                    _thermostat.OnUpdate -= OnUpdate;
                }
                _thermostat = lookupThermostat;
                _thermostat.OnUpdate += OnUpdate;
            }
            return _thermostat;
        }
        public void OnUpdate()
        {
            _ = OnUpdateAsync();
        }
        public async Task OnUpdateAsync()
        {


            if (string.IsNullOrWhiteSpace(DeviceName)) { return; }

            if (CurrentMode != Thermostat.Mode)
            {
                CurrentMode = Thermostat.Mode;
                await Dispatcher.SetTitleAsync($"{Thermostat.Mode}");
            }


        }
        public override async Task KeyDownAsync(int userDesiredState)
        {
            var success = false;
            if (CurrentMode != ThermostatMode.OFF)
            {
                success = Thermostat.SetMode(ThermostatMode.OFF);
            }
            else
            {
                success = Thermostat.SetMode(ThermostatMode.COOL);
            }
            if (!success) { await Dispatcher.ShowAlertAsync(); return; }
            await Dispatcher.SetTitleAsync($"{Thermostat.Mode}");


        }
    }
}
