using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tech.Aerove.Tools.Nest.Models.WebCalls;

namespace Tech.Aerove.Tools.Nest
{
    internal static class DevicesResponseExtensions
    {
        //updates current object with new one
        public static void Update(this DevicesResponse original, DevicesResponse updated)
        {
            if (original.Devices == null) { original.Devices = new(); }
            foreach (var updatedDevice in updated.Devices)
            {
                var oldDevice = original.Devices.FirstOrDefault(x => x.Name == updatedDevice.Name);
                if (oldDevice == null)
                {
                    original.Devices.Add(updatedDevice);
                    continue;
                }
                oldDevice.Traits = updatedDevice.Traits;
                oldDevice.ParentRelations = updatedDevice.ParentRelations;
            }
        }
    }
}
