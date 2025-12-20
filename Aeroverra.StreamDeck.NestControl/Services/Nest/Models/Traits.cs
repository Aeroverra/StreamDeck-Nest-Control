namespace Aeroverra.StreamDeck.NestControl.Services.Nest.Models
{
    using Newtonsoft.Json.Linq;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    public static class TraitExtensions
    {
        // Helper to extract a strong type from the "Traits" dictionary
        public static T GetTrait<T>(this IDictionary<string, object> traits, string traitName)
        {
            if (traits.TryGetValue(traitName, out var value))
            {
                // Case 1: System.Text.Json (What you coded for)
                if (value is JsonElement element)
                {
                    return element.Deserialize<T>();
                }

                // Case 2: Newtonsoft.Json (What the debugger actually sees)
                if (value is JObject jObject)
                {
                    return jObject.ToObject<T>();
                }

                // Case 3: It's already the strong type (Rare, but possible)
                if (value is T typedValue)
                {
                    return typedValue;
                }
            }
            return default;
        }
    }
    // Trait: "sdm.devices.traits.Info"
    public class DeviceInfoTrait
    {
        [JsonPropertyName("customName")]
        public string CustomName { get; set; }
    }

    // Trait: "sdm.devices.traits.Connectivity"
    public class ConnectivityTrait
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } // "ONLINE", "OFFLINE"
    }
    // Trait: "sdm.devices.traits.ThermostatMode"
    public class ThermostatModeTrait
    {
        [JsonPropertyName("mode")]
        public ThermostatMode Mode { get; set; } // "HEAT", "COOL", "HEATCOOL", "OFF"

        [JsonPropertyName("availableModes")]
        public string[] AvailableModes { get; set; }
    }

    public enum ThermostatMode
    {
        OFF, COOL, HEAT, HEATCOOL
    }

    // Trait: "sdm.devices.traits.ThermostatTemperatureSetpoint"
    public class ThermostatSetpointTrait
    {
        [JsonPropertyName("heatCelsius")]
        public decimal HeatCelsius { get; set; }

        [JsonPropertyName("coolCelsius")]
        public decimal CoolCelsius { get; set; }
    }

    // Trait: "sdm.devices.traits.Temperature"
    public class TemperatureTrait
    {
        [JsonPropertyName("ambientTemperatureCelsius")]
        public decimal AmbientTemperatureCelsius { get; set; }
    }
    // Trait: "sdm.devices.traits.CameraMotion"
    public class CameraMotionTrait
    {
        // Events often just send an empty object "{}" to signify the event occurred,
        // or sometimes an ID depending on the specific event version.
        [JsonPropertyName("eventSessionId")]
        public string EventSessionId { get; set; }
    }

    // Trait: "sdm.devices.traits.CameraPerson"
    public class CameraPersonTrait
    {
        [JsonPropertyName("eventSessionId")]
        public string EventSessionId { get; set; }
    }
}
