using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tech.Aerove.Tools.Nest.Models.WebCalls;

namespace Tech.Aerove.Tools.Nest.Models
{
    public abstract class NestDevice
    {
        public event Action OnUpdate;
        protected readonly NestClient NestClient;
        protected readonly string DeviceName;
        public NestDevice(NestClient nestClient, string deviceName)
        {
            NestClient = nestClient;
            DeviceName = deviceName;
            NestClient.OnDevicesUpdated += OnDevicesUpdated;
        }

        private void OnDevicesUpdated(object? sender, string name)
        {
            if(name == DeviceName)
            {
                OnUpdate?.Invoke();
            }
        }

    }
}
