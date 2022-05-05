using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tech.Aerove.StreamDeck.Client.Actions;
using Tech.Aerove.StreamDeck.NestControl.Extensions;
using Tech.Aerove.StreamDeck.NestControl.Models.GoogleApi;

namespace Tech.Aerove.StreamDeck.NestControl.Actions
{

    [PluginAction("tech.aerove.streamdeck.nestcontrol.temperatureup")]
    public class TemperatureUp : ActionBase
    {
        private List<Device> Devices { get { return JsonConvert.DeserializeObject<List<Device>>(Context.GlobalSettings["devices"].ToString()); } }
        private string DeviceName { get { return $"{Context.Settings["device"]}"; } }

        private readonly ExampleService _handler;
        private readonly ILogger<TemperatureUp> _logger;
        public TemperatureUp(ILogger<TemperatureUp> logger, ExampleService handler)
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
                try
                {
                    var device = Devices.SingleOrDefault(x => x.Name == DeviceName);
                    var currentSet = device.Traits.SdmDevicesTraitsThermostatTemperatureSetpoint.CoolCelsius;
                    currentSet = Math.Round(currentSet.ToFahrenheit(), 0);
                    await Dispatcher.SetTitleAsync($"{currentSet}");
                }
                catch (Exception)
                {

                }
            }
        }
        public override async Task KeyDownAsync(int userDesiredState)
        {
            var response = _handler.SetTempUp(int.Parse($"{Context.Settings["temperatureStep"]}"), $"{Context.Settings["device"]}");
            await Dispatcher.SetTitleAsync($"{response}");
        }
    }
}
