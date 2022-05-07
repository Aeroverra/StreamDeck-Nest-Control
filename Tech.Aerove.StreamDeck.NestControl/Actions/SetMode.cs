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
        private ThermostatDevice Thermostat => _handler.GetDevice(DeviceName);
        private int TemperatureStep => int.Parse($"{Context.Settings["temperatureStep"]}");
        private ThermostatMode CurrentMode = ThermostatMode.OFF;

        private readonly ExampleService _handler;
        private readonly ILogger<TemperatureUp> _logger;
        public SetMode(ILogger<TemperatureUp> logger, ExampleService handler)
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
                    if (CurrentMode != Thermostat.Mode)
                    {
                        CurrentMode = Thermostat.Mode;
                        await Dispatcher.SetTitleAsync($"{Thermostat.Mode}");
                    }
                }
                catch (Exception)
                {

                }
            }
        }
        public override async Task KeyDownAsync(int userDesiredState)
        {
            var success = false;
            if(CurrentMode != ThermostatMode.OFF)
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
