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
        public decimal SetPoint => Device.GetTemperatureSetPoint(true);
        public decimal CurrentTemperature => Device.GetCurrentTemperature(true);
    }
}
