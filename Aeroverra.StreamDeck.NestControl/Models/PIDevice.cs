using Aeroverra.StreamDeck.NestControl.Services.Nest;
using Google.Apis.SmartDeviceManagement.v1.Data;
using Newtonsoft.Json;

namespace Aeroverra.StreamDeck.NestControl.Models
{
    internal class PIDevice
    {
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; } = null!;

        [JsonProperty(PropertyName = "displayName")]
        public string DisplayName { get; set; } = null!;

        public static List<PIDevice> GetList(List<GoogleHomeEnterpriseSdmV1Device> devices)
        {
            devices = devices
                .Where(x => x.Type == NestConstants.DEVICE_TYPE_THERMOSTAT)
                .ToList();

            var piDevices = new List<PIDevice>();
            foreach (var device in devices)
            {
                var thermostatMode = device.GetDeviceInfo();

                var piDevice = new PIDevice
                {
                    Name = device.Name,
                    DisplayName = thermostatMode.CustomName
                };
                piDevices.Add(piDevice);
            }
            return piDevices;
        }
    }
}
