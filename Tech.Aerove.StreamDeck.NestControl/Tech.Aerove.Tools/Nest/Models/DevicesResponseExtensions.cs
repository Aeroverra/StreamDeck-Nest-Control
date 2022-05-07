using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public static decimal GetTemperatureSetPoint(this Device device, bool convertScale = false)
        {
            var mode = device.GetMode();
            if (mode != ThermostatMode.COOL && mode != ThermostatMode.HEAT) { return 0; }
            decimal setPoint = 0;
            if (mode == ThermostatMode.COOL)
            {
                setPoint = device.Traits.SdmDevicesTraitsThermostatTemperatureSetpoint.CoolCelsius;
            }
            else
            {
                setPoint = device.Traits.SdmDevicesTraitsThermostatTemperatureSetpoint.HeatCelsius;
            }
            if (convertScale && device.GetTemperatureScale() == TemperatureScale.FAHRENHEIT)
            {
                setPoint = setPoint.ToFahrenheit();
            }
            return setPoint;
        }
        public static int GetTemperatureSetPointAsInt(this Device device, bool convertScale = false)
        {
            var setPoint = device.GetTemperatureSetPoint(convertScale);
            return Convert.ToInt32(Math.Round(setPoint));
        }

        public static void SetTemperatureSetPoint(this Device device, ThermostatMode mode, decimal value)
        {
            if (mode == ThermostatMode.COOL)
            {
                device.Traits.SdmDevicesTraitsThermostatTemperatureSetpoint.CoolCelsius = value;
            }
            else
            {
                device.Traits.SdmDevicesTraitsThermostatTemperatureSetpoint.HeatCelsius = value;
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
