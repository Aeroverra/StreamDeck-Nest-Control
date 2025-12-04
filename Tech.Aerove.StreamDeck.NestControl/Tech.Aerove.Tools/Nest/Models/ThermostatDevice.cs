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
        public string SetPointRender
        {
            get
            {
                var mode = Device.GetMode();
                var setPoint = Device.GetTemperatureSetPoint(true);
                if (mode == ThermostatMode.COOL)
                {
                    return setPoint.CoolCelsius.ToString("F0");
                }
                else if (mode == ThermostatMode.HEAT)
                {
                    return setPoint.HeatCelsius.ToString("F0");
                }
                else if (mode == ThermostatMode.HEATCOOL)
                {
                    return $"{setPoint.CoolCelsius.ToString("F0")}-{setPoint.HeatCelsius.ToString("F0")}";
                }
                return "Err";
            }
        }
        public SdmDevicesTraitsThermostatTemperatureSetpoint SetPointsExact => Device.GetTemperatureSetPoint(true);
        public int CurrentTemperature => Device.GetCurrentTemperatureAsInt(true);
        public decimal CurrentTemperatureExact => Device.GetCurrentTemperature(true);
        public string Name => Device.GetName();
        public string DisplayName => Device.GetDisplayName();


        public bool SetMode(ThermostatMode mode)
        {
            return NestClient.SetMode(this, mode);
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
