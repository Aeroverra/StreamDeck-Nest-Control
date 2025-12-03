using Newtonsoft.Json;
using Tech.Aerove.StreamDeck.NestControl;
using Tech.Aerove.Tools.Nest.Models.WebCalls;

namespace Tech.Aerove.Tools.Nest
{
    internal static class DevicesResponseExtensions
    {
        //updates current object with new one
        public static void Update(this DevicesResponse original, DevicesResponse updated)
        {
            bool hadError = false;
            var error = "Inner Foreach Error" + Environment.NewLine;
            Exception lastException = null;
            if (original.Devices == null) { original.Devices = new(); }
            try
            {
                foreach (var updatedDevice in updated.Devices)
                {
                    try
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
                    catch (Exception e)
                    {
                        error += e.ToString() + Environment.NewLine;
                        hadError = true;
                        lastException = e;
                    }
                }
                if (hadError)
                {
                    _ = Communication.LogAsync(LogLevel.Critical, error += "\r\njson:\r\n" + JsonConvert.SerializeObject(updated));
                    throw lastException;
                }
            }
            catch (Exception e)
            {
                if (!hadError)
                {
                    _ = Communication.LogAsync(LogLevel.Critical, "json Outer:\r\n" + JsonConvert.SerializeObject(updated));

                }
                throw e;
            }

        }
    }
}
