using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tech.Aerove.StreamDeck.Client.Actions;
using Tech.Aerove.StreamDeck.NestControl.Models.GoogleApi;
using Tech.Aerove.StreamDeck.NestControl.Tech.Aerove.Tools.Nest;
using Tech.Aerove.Tools.Nest;

//project id
//dcd2fda3-c5b5-47a1-a6c6-9fe88ea6bf9d
//id
//52480091894-aks3uqjcoc8t0aceht0k21qsc0f5vqt8.apps.googleusercontent.com
//secret
//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
namespace Tech.Aerove.StreamDeck.NestControl.Actions
{
    [PluginAction("tech.aerove.streamdeck.nestcontrol.temperaturedown")]
    public class TemperatureDown : ActionBase
    {
        private List<Device> Devices { get { return JsonConvert.DeserializeObject<List<Device>>(Context.GlobalSettings["devices"].ToString()); } }
        private string DeviceName { get { return $"{Context.Settings["device"]}"; } }

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
            var tempStep = int.Parse($"{Context.Settings["temperatureStep"]}");
            var response = _handler.SetTempDown(tempStep, $"{Context.Settings["device"]}");
            await Dispatcher.SetTitleAsync($"{response}");
        }
    }
}
