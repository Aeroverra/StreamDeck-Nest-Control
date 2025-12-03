using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Tech.Aerove.StreamDeck.NestControl.Tech.Aerove.Tools.Nest.Models.WebCalls
{
    internal class CommandBody
    {
        [JsonProperty("command")]
        public string Command { get; set; }
        [JsonProperty("params")]
        public JObject Params { get; set; } = new JObject();
    }
}
