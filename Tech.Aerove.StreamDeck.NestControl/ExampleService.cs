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
using Tech.Aerove.StreamDeck.NestControl.Tech.Aerove.Tools.Nest;
using Tech.Aerove.Tools.Nest;
using Tech.Aerove.Tools.Nest.Models;

namespace Tech.Aerove.StreamDeck.NestControl
{

    public class ExampleService : BackgroundService
    {
        private readonly ILogger<ExampleService> _logger;
        private readonly EventManager _eventsManager;
        private readonly IElgatoDispatcher _dispatcher;
        private readonly IConfiguration _config;

        private JObject GlobalSettings = new JObject();
        private string ClientId { get { return $"{GlobalSettings["clientId"]}"; } }
        private string ClientSecret { get { return $"{GlobalSettings["clientSecret"]}"; } }
        private string ProjectId { get { return $"{GlobalSettings["projectId"]}"; } }
        private string Scope { get { return $"{GlobalSettings["scope"]}"; } }
        private string RefreshToken { get { return $"{GlobalSettings["refreshToken"]}"; } }
        private bool? IsSetup { get { return GlobalSettings["setup"]?.ToObject<bool>(); } }

        private NestClient Client { get; set; }
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

            if (e.payload["Reset"]?.ToObject<bool>() == true)
            {
                Reset();
            }
            if (e.payload["Setup"]?.ToObject<bool>() == true)
            {
                Setup();
            }
        }
        private void Reset()
        {
            GlobalSettings["setup"] = false; GlobalSettings["piDevices"] = null;
            _dispatcher.SetGlobalSettings(GlobalSettings);
            _dispatcher.SendToPropertyInspector(LastContext, LastUUID, new { Update = true });
            return;
        }
        private void Setup()
        {
            _dispatcher.GetGlobalSettings();
            Client = new NestClient(ClientId, ClientSecret, ProjectId);
            var url = Client.GetAccountLinkUrl("http://localhost:20777");
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
        private bool firstLoad = true;
        protected void OnDidRecieveGlobalSettings(object? sender, DidReceiveGlobalSettingsEvent e)
        {
            GlobalSettings = e.Payload.Settings;
            if (firstLoad & IsSetup == true)
            {
                Client = new NestClient(ClientId, ClientSecret, ProjectId, RefreshToken, Scope);
            }
            firstLoad = false;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_config.GetValue<bool>("DevLogParametersOnly", false)) { return; }
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
            var refreshToken = Client.FinishSetup(code, scope);
            var responseString = "Error please check your settings";
            if (refreshToken != null)
            {
                UpdateDevices();
                GlobalSettings["code"] = code;
                GlobalSettings["scope"] = scope;
                GlobalSettings["setup"] = true;
                GlobalSettings["refreshToken"] = refreshToken;
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

        public bool UpdateDevices()
        {
            var piDevices = PIDevice.GetList(Client.GetThermostats());
            GlobalSettings["piDevices"] = JsonConvert.SerializeObject(piDevices);
            _dispatcher.SetGlobalSettings(GlobalSettings);
            _dispatcher.SendToPropertyInspector(LastContext, LastUUID, new { Update = true });
            return true;
        }

        public ThermostatDevice GetDevice(string name)
        {
            return Client.GetThermostat(name);
        }
    }
}