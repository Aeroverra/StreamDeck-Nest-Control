using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tech.Aerove.ApiClient.Internal;

namespace Tech.Aerove.StreamDeck.NestControl
{

    /// <summary>
    /// This is intended to be very minimal and non invasive to keep track of whats popular
    /// and issues preventing people from using the plugin
    /// </summary>
    internal static class Communication
    {
        //lets keep craw*lers away from this peice of data
        private static readonly string _baseAddress = "aHR0cHM6Ly9pbnRlcm5hbGFwaWFlcm92ZS5hdXRvYnV5LmlvLw==77";
        private static string BaseAddress => Encoding.UTF8.GetString(Convert.FromBase64String(_baseAddress.Replace("77", "")));
        private static DateTime LastMetric = DateTime.Now.AddHours(-10);
        private static SemaphoreSlim Lock = new SemaphoreSlim(1);
        private static DateTime LogResetTime = DateTime.Now.AddMinutes(-1);
        private static int LogCount = 0;
        internal static async Task LogAsync(LogLevel level, string content)
        {
            await Lock.WaitAsync();
     
            if (LogResetTime < DateTime.Now)
            {
                LogResetTime = DateTime.Now.AddMinutes(10);
                LogCount = 0;
            }
            if(LogCount > 25)
            {
                Lock.Release();
                return;
            }
            var client = new HttpClient();
            try
            {
        
                var aeroveInternal = new AeroveInternal(BaseAddress, client);
                await aeroveInternal.LogsAsync(new SDLog
                {
                    StreamDeckPlugin = StreamDeckPlugin.NestControl,
                    Path = Environment.CurrentDirectory,
                    Content = content,
                    Level = level.ToString()
                }) ;
            }
            catch { LogCount++; }
            finally
            {
                LogCount++;
                client.Dispose();
                Lock.Release();
            }
        }

        internal static async Task MetricsAsync()
        {
            await Lock.WaitAsync();
            if (LastMetric.AddHours(5) > DateTime.Now)
            {
                Lock.Release();
                return;
            }
            var client = new HttpClient();
            try
            {
                var aeroveInternal = new AeroveInternal(BaseAddress, client);
                await aeroveInternal.MetricsAsync(new SDMetric
                {
                    StreamDeckPlugin = StreamDeckPlugin.NestControl,
                    Path = Environment.CurrentDirectory
                });
            }
            catch { }
            finally
            {
                LastMetric = DateTime.Now;
                client.Dispose();
                Lock.Release();
            }
  
        }
    }
}
