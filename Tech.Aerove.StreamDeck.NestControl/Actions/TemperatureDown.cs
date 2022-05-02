using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tech.Aerove.StreamDeck.Client.Actions;

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
        private readonly ILogger<TemperatureDown> _logger;

        public TemperatureDown(ILogger<TemperatureDown> logger)
        {
            _logger = logger;
        }
    }
}
