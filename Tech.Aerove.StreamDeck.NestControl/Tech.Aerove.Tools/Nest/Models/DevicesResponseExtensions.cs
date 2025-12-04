using Tech.Aerove.Tools.Nest.Models.WebCalls;

namespace Tech.Aerove.Tools.Nest.Models
{
    internal static class DevicesResponseExtensions
    {
        public static Device GetDevice(this DevicesResponse devicesResponse, string name)
        {
            return devicesResponse.Devices.FirstOrDefault(x => x.Name.ToLower() == name.ToLower());
        }

        public static ThermostatMode GetMode(this Device device)
        {
            return (ThermostatMode)Enum.Parse(typeof(ThermostatMode), device.Traits.SdmDevicesTraitsThermostatMode.Mode);
        }

        public static void SetMode(this Device device, ThermostatMode mode)
        {
            device.Traits.SdmDevicesTraitsThermostatMode.Mode = $"{mode}";
        }

        public static TemperatureScale GetTemperatureScale(this Device device)
        {
            return (TemperatureScale)Enum.Parse(typeof(TemperatureScale), device.Traits.SdmDevicesTraitsSettings.TemperatureScale);
        }

        public static SdmDevicesTraitsThermostatTemperatureSetpoint GetTemperatureSetPoint(this Device device, bool convertScale = false)
        {
            var setPoint = device.Traits.SdmDevicesTraitsThermostatTemperatureSetpoint.Clone();
            if (convertScale && device.GetTemperatureScale() == TemperatureScale.FAHRENHEIT)
            {
                setPoint.CoolCelsius = setPoint.CoolCelsius.ToFahrenheit();
                setPoint.HeatCelsius = setPoint.HeatCelsius.ToFahrenheit();
            }
            return setPoint;
        }

        public static void SetTemperatureSetPoint(this Device device, ThermostatMode mode, SdmDevicesTraitsThermostatTemperatureSetpoint value)
        {
            if (mode == ThermostatMode.COOL || mode == ThermostatMode.HEATCOOL)
            {
                device.Traits.SdmDevicesTraitsThermostatTemperatureSetpoint.CoolCelsius = value.CoolCelsius;
            }
            if (mode == ThermostatMode.HEAT || mode == ThermostatMode.HEATCOOL)
            {
                device.Traits.SdmDevicesTraitsThermostatTemperatureSetpoint.HeatCelsius = value.HeatCelsius;
            }
        }

        public static decimal GetCurrentTemperature(this Device device, bool convertScale = false)
        {
            var current = device.Traits.SdmDevicesTraitsTemperature.AmbientTemperatureCelsius;
            if (convertScale && device.GetTemperatureScale() == TemperatureScale.FAHRENHEIT)
            {
                current = current.ToFahrenheit();
            }
            return current;
        }
        public static int GetCurrentTemperatureAsInt(this Device device, bool convertScale = false)
        {
            var currentTemp = device.GetCurrentTemperature(convertScale);
            return Convert.ToInt32(Math.Round(currentTemp));
        }

        public static ThermostatStatus GetStatus(this Device device)
        {
            return (ThermostatStatus)Enum.Parse(typeof(ThermostatStatus), device.Traits.SdmDevicesTraitsThermostatHvac.Status);
        }

        public static string GetName(this Device device)
        {
            return device.Name;
        }

        public static string GetDisplayName(this Device device)
        {
            var displayName = device.Traits.SdmDevicesTraitsInfo.CustomName;
            if (String.IsNullOrWhiteSpace(displayName) && device.ParentRelations.Any())
            {
                displayName = device.ParentRelations[0].DisplayName;
            }
            return displayName;
        }
    }
}
