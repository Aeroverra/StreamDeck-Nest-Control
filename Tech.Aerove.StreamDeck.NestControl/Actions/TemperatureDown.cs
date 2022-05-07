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
        private ThermostatDevice Thermostat => _handler.GetDevice(DeviceName);
        private int TemperatureStep => int.Parse($"{Context.Settings["temperatureStep"]}");
        private decimal CurrentSetPoint = 0;

        private readonly ILogger<TemperatureDown> _logger;
        private readonly ExampleService _handler;
        public TemperatureDown(ILogger<TemperatureDown> logger, ExampleService handler)
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
                        await Dispatcher.SetTitleAsync($"{Thermostat.SetPoint}");
                    }
                }
                catch (Exception)
                {

                }
            }
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
            await Dispatcher.SetTitleAsync($"{Thermostat.SetPoint}");
        }
    }
}
