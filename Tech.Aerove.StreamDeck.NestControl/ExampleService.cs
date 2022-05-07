using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using System.Web;
using Tech.Aerove.StreamDeck.Client;
using Tech.Aerove.StreamDeck.Client.Events;
using Tech.Aerove.StreamDeck.NestControl.Models;
using Tech.Aerove.StreamDeck.NestControl.Models.GoogleApi;
using Tech.Aerove.StreamDeck.NestControl.Tech.Aerove.Tools.Nest;
using Tech.Aerove.Tools.Nest;

namespace Tech.Aerove.StreamDeck.NestControl
{

    public class ExampleService : BackgroundService
    {
        private readonly ILogger<ExampleService> _logger;
        private readonly EventManager _eventsManager;
        private readonly IElgatoDispatcher _dispatcher;
        private readonly IConfiguration _config;
        //user set
        private string ClientId { get { return $"{GlobalSettings["clientId"]}"; } }
        private string ClientSecret { get { return $"{GlobalSettings["clientSecret"]}"; } }
        private string ProjectId { get { return $"{GlobalSettings["projectId"]}"; } }

        private string AccessToken { get { return $"{GlobalSettings["accessToken"]}"; } }
        private string RefreshToken { get { return $"{GlobalSettings["refreshToken"]}"; } }
        private List<Device> Devices { get { return JsonConvert.DeserializeObject<List<Device>>(GlobalSettings["devices"].ToString()); } }
        private DateTime Expires { get { return GlobalSettings["expires"].ToObject<DateTime>(); } }

        private bool? IsSetup { get { return GlobalSettings["setup"]?.ToObject<bool>(); } }
        private JObject GlobalSettings = new JObject();
        private string LastContext = "";
        private string LastUUID = "";
        public ExampleService(ILogger<ExampleService> logger, EventManager eventsManager, IElgatoDispatcher dispatcher, IConfiguration config)
        {
            _logger = logger;
            _eventsManager = eventsManager;
            _dispatcher = dispatcher;
            _config = config;
            _eventsManager.OnSendToPlugin += OnSendToPlugin;
            _eventsManager.OnDidReceiveGlobalSettings += OnDidRecieveGlobalSettings;
        }

        protected void OnSendToPlugin(object? sender, SendToPluginEvent e)
        {
            LastContext = e.Context;
            LastUUID = e.Action;
            var reset = e.payload["Reset"]?.ToObject<bool>();
            if (reset == true)
            {
                GlobalSettings["code"] = null; GlobalSettings["setup"] = false; GlobalSettings["piDevices"] = null;
                _dispatcher.SetGlobalSettings(GlobalSettings);
                _dispatcher.SendToPropertyInspector(LastContext, LastUUID, new { Update = true });
                return;
            }
            var setup = e.payload["Setup"]?.ToObject<bool>();
            if (setup == true)
            {
                _dispatcher.GetGlobalSettings();
                var url = GoogleApi.GetAccountLinkUrl(ProjectId, ClientId);
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    url = url.Replace("&", "^&");
                    Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start("open", url);
                }
            }




        }

        private bool firstLoad = true;
        protected void OnDidRecieveGlobalSettings(object? sender, DidReceiveGlobalSettingsEvent e)
        {
            GlobalSettings = e.Payload.Settings;
            if (firstLoad)
            {
                firstLoad = false;
                if (IsSetup == true)
                {
                    UpdateDevices();
                }
            }

        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_config.GetValue<bool>("DevLogParametersOnly", false))
            {
                return;
            }
            using (HttpListener listener = new HttpListener())
            {
                listener.Prefixes.Add("http://localhost:20777/");
                listener.Start();
                while (!stoppingToken.IsCancellationRequested)
                {
                    // Note: The GetContext method blocks while waiting for a request.
                    HttpListenerContext context = await listener.GetContextAsync();

                    //Process request without blocking in order to handle multiple requests if needed
                    _ = Task.Run(() => ProcessRequest(context, stoppingToken));
                }
            }
        }

        protected async Task ProcessRequest(HttpListenerContext _http, CancellationToken stoppingToken)
        {
            HttpListenerRequest request = _http.Request;

            var query = _http.Request.QueryString;
            var code = query["code"]?.ToString();
            var scope = query["scope"]?.ToString();
            var responseString = "Error please check your settings";
            if (code != null && SetFirstAccessToken(code) && UpdateDevices())
            {
                GlobalSettings["code"] = code;
                GlobalSettings["setup"] = true;
                _dispatcher.SetGlobalSettings(GlobalSettings);
                _dispatcher.SendToPropertyInspector(LastContext, LastUUID, new { Update = true });
                responseString = "Success! You can now close this window.";
            }
            //Read Raw body
            var rawBody = await new StreamReader(request.InputStream).ReadToEndAsync();

            //Write Response
            HttpListenerResponse response = _http.Response;
            response.StatusCode = 200;
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
            response.ContentLength64 = buffer.Length;
            System.IO.Stream output = response.OutputStream;
            await output.WriteAsync(buffer, 0, buffer.Length);
            output.Close();


        }

        public bool SetFirstAccessToken(string code)
        {
            var response = GoogleApi.GetAccessToken(ClientId, ClientSecret, code);
            if (response == null) { return false; }

            GlobalSettings["accessToken"] = response.AccessToken;
            GlobalSettings["refreshToken"] = response.RefreshToken;
            GlobalSettings["expires"] = DateTime.Now.AddSeconds(response.ExpiresIn - 10);
            return true;
        }

        public bool UpdateDevices()
        {
            UpdateAccessToken();
            var response = GoogleApi.GetDevices(ProjectId, AccessToken);
            var thermostats = response.Devices
                .Where(x => x.Type == "sdm.devices.types.THERMOSTAT")
                .ToList();

            var piDevices = PIDevice.GetList(thermostats);

            GlobalSettings["devices"] = JsonConvert.SerializeObject(thermostats);
            GlobalSettings["piDevices"] = JsonConvert.SerializeObject(piDevices);

            _dispatcher.SetGlobalSettings(GlobalSettings);
            _dispatcher.SendToPropertyInspector(LastContext, LastUUID, new { Update = true });

            return true;
        }

        public void UpdateAccessToken()
        {
            Console.WriteLine(AccessToken);
            if (Expires > DateTime.Now)
            {
                return;
            }
            var response = GoogleApi.RefreshToken(ClientId, ClientSecret, RefreshToken);
            GlobalSettings["accessToken"] = response.AccessToken;
            GlobalSettings["expires"] = DateTime.Now.AddSeconds(response.ExpiresIn - 10);
            _dispatcher.SetGlobalSettings(GlobalSettings);
            _dispatcher.SendToPropertyInspector(LastContext, LastUUID, new { Update = true });

        }
      
        public bool SetMode(string mode, string name)
        {
            UpdateAccessToken();
            var devices = Devices;
            var device = devices.Where(x => x.Name == name).SingleOrDefault();
            var success = GoogleApi.SetMode(device, AccessToken, mode);
            if (success)
            {
                device.Traits.SdmDevicesTraitsThermostatMode.Mode = mode;
                _dispatcher.SetGlobalSettings(GlobalSettings);
                return true;
            }
            return false;
        }

        private static SemaphoreSlim Lock = new SemaphoreSlim(1);
       
        public decimal SetTempDown(int amount, string name)
        {
            Lock.Wait();
            UpdateAccessToken();
            var devices = Devices;
            var device = devices.Where(x => x.Name == name).SingleOrDefault();
            var mode = device.Traits.SdmDevicesTraitsThermostatMode.Mode;
            if (mode == "OFF")
            {
                SetMode("COOL", name);
                mode = "COOL";
            }
            var currentSet = device.Traits.SdmDevicesTraitsThermostatTemperatureSetpoint.CoolCelsius;
            currentSet = currentSet.ToFahrenheit();
            currentSet -= amount;
            currentSet = currentSet.ToCelsius();

            var success = GoogleApi.SetTemp(device, AccessToken, mode, currentSet);
            if (success)
            {
                device.Traits.SdmDevicesTraitsThermostatTemperatureSetpoint.CoolCelsius = currentSet;
                GlobalSettings["devices"] = JsonConvert.SerializeObject(devices);
                _dispatcher.SetGlobalSettings(GlobalSettings);
            }
            Lock.Release();
            return Math.Round(currentSet.ToFahrenheit(), 0);
    
        }
      
        public decimal SetTempUp(int amount, string name)
        {
            Lock.Wait();
            UpdateAccessToken();
            var devices = Devices;
            var device = devices.Where(x => x.Name == name).SingleOrDefault();
            var mode = device.Traits.SdmDevicesTraitsThermostatMode.Mode;
            if (mode == "OFF")
            {
                SetMode("HEAT", name);
                mode = "HEAT";
            }
            var currentSet = device.Traits.SdmDevicesTraitsThermostatTemperatureSetpoint.CoolCelsius;
            currentSet = currentSet.ToFahrenheit();
            currentSet += amount;
            currentSet = currentSet.ToCelsius();
            var success = GoogleApi.SetTemp(device, AccessToken, mode, currentSet);
            if (success)
            {
                device.Traits.SdmDevicesTraitsThermostatTemperatureSetpoint.CoolCelsius = currentSet;
                GlobalSettings["devices"] = JsonConvert.SerializeObject(devices);
                _dispatcher.SetGlobalSettings(GlobalSettings);
            }
            Lock.Release();
            return Math.Round(currentSet.ToFahrenheit(), 0);
       
        }


    }
}