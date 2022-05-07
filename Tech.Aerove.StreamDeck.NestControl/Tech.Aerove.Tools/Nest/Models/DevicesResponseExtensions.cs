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
        public static TemperatureScale GetTemperatureScale(this Device device)
        {
            return (TemperatureScale)Enum.Parse(typeof(TemperatureScale), device.Traits.SdmDevicesTraitsSettings.TemperatureScale);
        }
        public static decimal GetTemperatureSetPoint(this Device device, bool convertScale = false)
        {
            var mode = device.GetMode();
            if (mode != (ThermostatMode.COOL | ThermostatMode.HEAT)) { return 0; }
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
        public static decimal GetCurrentTemperature(this Device device, bool convertScale = false)
        {
            var current = device.Traits.SdmDevicesTraitsTemperature.AmbientTemperatureCelsius;
            if (convertScale && device.GetTemperatureScale() == TemperatureScale.FAHRENHEIT)
            {
                current = current.ToFahrenheit();
            }
            return current;
        }
        public static ThermostatStatus GetStatus(this Device device)
        {
            return (ThermostatStatus)Enum.Parse(typeof(ThermostatStatus), device.Traits.SdmDevicesTraitsThermostatHvac.Status);
        }
    }
}
