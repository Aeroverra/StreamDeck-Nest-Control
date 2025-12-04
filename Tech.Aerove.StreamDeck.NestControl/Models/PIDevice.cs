using Newtonsoft.Json;
using Tech.Aerove.Tools.Nest.Models;

namespace Aeroverra.StreamDeck.NestControl.Models
{
    internal class PIDevice
    {
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }
        [JsonProperty(PropertyName = "displayName")]
        public string DisplayName { get; set; }

        public static List<PIDevice> GetList(List<ThermostatDevice> devices)
        {
            var piDevices = new List<PIDevice>();
            foreach (var device in devices)
            {
                var piDevice = new PIDevice
                {
                    Name = device.Name,
                    DisplayName = device.DisplayName
                };
                piDevices.Add(piDevice);
            }
            return piDevices;
        }

    }

}
