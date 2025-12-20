using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Aeroverra.StreamDeck.NestControl.Services.Nest.Models
{
    internal class CommandBody
    {
        [JsonProperty("command")]
        public string Command { get; set; }
        [JsonProperty("params")]
        public JObject Params { get; set; } = new JObject();
    }
}
