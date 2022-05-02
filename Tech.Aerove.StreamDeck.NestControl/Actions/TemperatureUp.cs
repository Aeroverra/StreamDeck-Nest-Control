using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tech.Aerove.StreamDeck.Client.Actions;

namespace Tech.Aerove.StreamDeck.NestControl.Actions
{

    [PluginAction("tech.aerove.streamdeck.nestcontrol.temperatureup")]
    public class TemperatureUp : ActionBase
    {
        private readonly ILogger<TemperatureUp> _logger;

        public TemperatureUp(ILogger<TemperatureUp> logger)
        {
            _logger = logger;
        }
    }
}
