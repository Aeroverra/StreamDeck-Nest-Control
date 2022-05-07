using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tech.Aerove.Tools.Nest.Models.WebCalls;

namespace Tech.Aerove.Tools.Nest.Models
{
    public class ThermostatDevice : NestDevice
    {
        public ThermostatDevice(NestClient nestClient, string deviceName) : base(nestClient, deviceName)
        {
        }
        private Device Device => NestClient.DevicesResponse.GetDevice(DeviceName);
        public ThermostatMode Mode => Device.GetMode();
        public ThermostatStatus Status => Device.GetStatus();
        public TemperatureScale Scale => Device.GetTemperatureScale();
        public int SetPoint => Device.GetTemperatureSetPointAsInt(true);
        public decimal SetPointExact => Device.GetTemperatureSetPoint(true);
        public int CurrentTemperature => Device.GetCurrentTemperatureAsInt(true);
        public decimal CurrentTemperatureExact => Device.GetCurrentTemperature(true);
        public string Name => Device.GetName();
        public string DisplayName => Device.GetDisplayName();


        public bool SetMode(ThermostatMode mode)
        {
            return NestClient.SetMode(this, mode);
        }

        public bool SetTemp(decimal value)
        {
            return NestClient.SetTemp(this, value);
        }

        public bool SetTempUp(int value)
        {
            return NestClient.SetTempUp(this, value);
        }

        public bool SetTempDown(int value)
        {
            return NestClient.SetTempDown(this, value);
        }

    }
}
