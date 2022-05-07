using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
