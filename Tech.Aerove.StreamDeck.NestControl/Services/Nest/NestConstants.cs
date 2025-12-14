using System;
using System.Collections.Generic;
using System.Text;

namespace Aeroverra.StreamDeck.NestControl.Services.Nest
{
    public class NestConstants
    {
        public const string DEVICE_TYPE_THERMOSTAT = "sdm.devices.types.THERMOSTAT";

        public const string COMMAND_SET_COOLING_TEMPERATURE = "sdm.devices.commands.ThermostatTemperatureSetpoint.SetCool";
        public const string COMMAND_SET_COOLING_TEMPERATURE_PARAMETER = "coolCelsius";
        public const string COMMAND_SET_HEATING_TEMPERATURE = "sdm.devices.commands.ThermostatTemperatureSetpoint.SetHeat";
        public const string COMMAND_SET_HEATING_TEMPERATURE_PARAMETER = "heatCelsius";
        public const string COMMAND_SET_RANGE_TEMPERATURE = "sdm.devices.commands.ThermostatTemperatureSetpoint.SetRange";

        public const string TRAIT_DEVICE_INFO = "sdm.devices.traits.Info";
        public const string TRAIT_THERMOSTAT_MODE = "sdm.devices.traits.ThermostatMode";
        public const string TRAIT_THERMOSTAT_SETPOINT = "sdm.devices.traits.ThermostatTemperatureSetpoint";
        public const string TRAIT_THERMOSTAT_Temperature = "sdm.devices.traits.Temperature";

    }
}
