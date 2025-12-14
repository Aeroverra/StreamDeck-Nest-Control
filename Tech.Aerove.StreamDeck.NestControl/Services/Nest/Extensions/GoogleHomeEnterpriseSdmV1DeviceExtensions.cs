using Aeroverra.StreamDeck.NestControl.Services.Nest.Models;
using Google.Apis.SmartDeviceManagement.v1.Data;

namespace Aeroverra.StreamDeck.NestControl.Services.Nest.Extensions
{
    public static class GoogleHomeEnterpriseSdmV1DeviceExtensions
    {
        public static bool IsThermostat(this GoogleHomeEnterpriseSdmV1Device device)
        {
            return device.Type == NestConstants.DEVICE_TYPE_THERMOSTAT;
        }

        public static DeviceInfoTrait GetDeviceInfo(this GoogleHomeEnterpriseSdmV1Device device)
        {
            return device.Traits.GetTrait<DeviceInfoTrait>(NestConstants.TRAIT_DEVICE_INFO);
        }

        public static ThermostatModeTrait GetThermostatMode(this GoogleHomeEnterpriseSdmV1Device device)
        {
            return device.Traits.GetTrait<ThermostatModeTrait>(NestConstants.TRAIT_THERMOSTAT_MODE);
        }

        public static ThermostatSetpointTrait GetThermostatSetPoint(this GoogleHomeEnterpriseSdmV1Device device)
        {
            return device.Traits.GetTrait<ThermostatSetpointTrait>(NestConstants.TRAIT_THERMOSTAT_SETPOINT);
        }

        public static TemperatureTrait GetThermostatTemperature(this GoogleHomeEnterpriseSdmV1Device device)
        {
            return device.Traits.GetTrait<TemperatureTrait>(NestConstants.TRAIT_THERMOSTAT_Temperature);
        }
    }
}
