using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tech.Aerove.Tools.Nest.Models.WebCalls
{
    internal partial class DevicesResponse
    {
        [JsonProperty("devices")]
        public List<Device> Devices { get; set; }
    }

    internal partial class Device
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("assignee")]
        public string Assignee { get; set; }

        [JsonProperty("traits")]
        public Traits Traits { get; set; }

        [JsonProperty("parentRelations")]
        public List<ParentRelation> ParentRelations { get; set; }
    }

    internal partial class ParentRelation
    {
        [JsonProperty("parent")]
        public string Parent { get; set; }

        [JsonProperty("displayName")]
        public string DisplayName { get; set; }
    }

    internal partial class Traits
    {
        [JsonProperty("sdm.devices.traits.Info")]
        public SdmDevicesTraitsInfo SdmDevicesTraitsInfo { get; set; }

        [JsonProperty("sdm.devices.traits.Humidity")]
        public SdmDevicesTraitsHumidity SdmDevicesTraitsHumidity { get; set; }

        [JsonProperty("sdm.devices.traits.Connectivity")]
        public SdmDevicesTraits SdmDevicesTraitsConnectivity { get; set; }

        [JsonProperty("sdm.devices.traits.Fan")]
        public SdmDevicesTraitsFan SdmDevicesTraitsFan { get; set; }

        [JsonProperty("sdm.devices.traits.ThermostatMode")]
        public SdmDevicesTraitsThermostatMode SdmDevicesTraitsThermostatMode { get; set; }

        [JsonProperty("sdm.devices.traits.ThermostatEco")]
        public SdmDevicesTraitsThermostatEco SdmDevicesTraitsThermostatEco { get; set; }

        [JsonProperty("sdm.devices.traits.ThermostatHvac")]
        public SdmDevicesTraits SdmDevicesTraitsThermostatHvac { get; set; }

        [JsonProperty("sdm.devices.traits.Settings")]
        public SdmDevicesTraitsSettings SdmDevicesTraitsSettings { get; set; }

        [JsonProperty("sdm.devices.traits.ThermostatTemperatureSetpoint")]
        public SdmDevicesTraitsThermostatTemperatureSetpoint SdmDevicesTraitsThermostatTemperatureSetpoint { get; set; }

        [JsonProperty("sdm.devices.traits.Temperature")]
        public SdmDevicesTraitsTemperature SdmDevicesTraitsTemperature { get; set; }
    }

    internal partial class SdmDevicesTraits
    {
        [JsonProperty("status")]
        public string Status { get; set; }
    }

    internal partial class SdmDevicesTraitsFan
    {
        [JsonProperty("timerMode")]
        public string TimerMode { get; set; }
    }

    internal partial class SdmDevicesTraitsHumidity
    {
        [JsonProperty("ambientHumidityPercent")]
        public long AmbientHumidityPercent { get; set; }
    }

    internal partial class SdmDevicesTraitsInfo
    {
        [JsonProperty("customName")]
        public string CustomName { get; set; }
    }

    internal partial class SdmDevicesTraitsSettings
    {
        [JsonProperty("temperatureScale")]
        public string TemperatureScale { get; set; }
    }

    internal partial class SdmDevicesTraitsTemperature
    {
        [JsonProperty("ambientTemperatureCelsius")]
        public decimal AmbientTemperatureCelsius { get; set; }
    }

    internal partial class SdmDevicesTraitsThermostatEco
    {
        [JsonProperty("availableModes")]
        public List<string> AvailableModes { get; set; }

        [JsonProperty("mode")]
        public string Mode { get; set; }

        [JsonProperty("heatCelsius")]
        public double HeatCelsius { get; set; }

        [JsonProperty("coolCelsius")]
        public double CoolCelsius { get; set; }
    }

    internal partial class SdmDevicesTraitsThermostatMode
    {
        [JsonProperty("mode")]
        public string Mode { get; set; }

        [JsonProperty("availableModes")]
        public List<string> AvailableModes { get; set; }
    }

    internal partial class SdmDevicesTraitsThermostatTemperatureSetpoint
    {
        [JsonProperty("coolCelsius")]
        public decimal CoolCelsius { get; set; }
        [JsonProperty("heatCelsius")]
        public decimal HeatCelsius { get; set; }
    }
}
