using Aeroverra.StreamDeck.Client;
using Aeroverra.StreamDeck.Client.Events;
using Aeroverra.StreamDeck.NestControl.Models;
using Aeroverra.StreamDeck.NestControl.Services.Nest;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;

namespace Aeroverra.StreamDeck.NestControl.Services
{
    internal class CoreWorker : BackgroundService, IAsyncDisposable
    {
        private readonly ILogger<CoreWorker> _logger;
        private readonly EventManager _eventsManager;
        private readonly IElgatoDispatcher _dispatcher;
        private readonly IConfiguration _config;
        private readonly GlobalSettings _globalSettings;
        private readonly NestService _nestService;
        private readonly SemaphoreSlim Lock = new SemaphoreSlim(1);

        private string LastContext = "";
        private string LastUUID = "";
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private Task? ListenerTask = null;
        public bool isRunning = false;

        public CoreWorker(ILogger<CoreWorker> logger, EventManager eventsManager, IElgatoDispatcher dispatcher, IConfiguration config, GlobalSettings globalSettings, NestService nestService)
        {
            _logger = logger;
            _eventsManager = eventsManager;
            _dispatcher = dispatcher;
            _config = config;
            _globalSettings = globalSettings;
            _nestService = nestService;
            _eventsManager.OnSendToPlugin += OnSendToPlugin;
            _eventsManager.OnDidReceiveGlobalSettings += OnDidRecieveGlobalSettings;
            _nestService.OnConnected += OnConnected;
        }

        private void OnConnected(object? sender, EventArgs e)
        {
            isRunning = true;
            var piDevices = PIDevice.GetList(_nestService.Devices.ToList());
            _globalSettings.PiDevices = JsonConvert.SerializeObject(piDevices);
            _globalSettings.SubscriptionId = _nestService.SubscriptionId;
            _dispatcher.SetGlobalSettingsAsync(_globalSettings);
            _dispatcher.SendToPropertyInspector(LastContext, LastUUID, new { Update = true });

        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(1000, stoppingToken);
                }
            }
            catch (OperationCanceledException) { }

            await _cancellationTokenSource.CancelAsync();

            await _nestService.StopAsync();

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

        protected async void OnDidRecieveGlobalSettings(object? sender, DidReceiveGlobalSettingsEvent e)
        {
            await Lock.WaitAsync();
            await Task.Delay(1000);
            try
            {
                if (isRunning == false && _globalSettings.Setup == true)
                {
                    await _nestService.ConnectWithRefreshToken(_globalSettings.ProjectId!, _globalSettings.CloudProjectId!, _globalSettings.ClientId!, _globalSettings.ClientSecret!, _globalSettings.RefreshToken!, _globalSettings.SubscriptionId!, _cancellationTokenSource.Token);
                }
            }
            finally
            {
                Lock.Release();
            }
        }

        private async Task ListenForCallback()
        {
            using (HttpListener listener = new HttpListener())
            {
                listener.Prefixes.Add("http://localhost:20777/");
                listener.Start();
                while (!_cancellationTokenSource.IsCancellationRequested)
                {
                    // Note: The GetContext method blocks while waiting for a request.
                    HttpListenerContext context = await listener.GetContextAsync();

                    //Process request without blocking in order to handle multiple requests if needed
                    _ = Task.Run(() => ProcessRequest(context, _cancellationTokenSource.Token));
                }
            }
        }

        private void Reset()
        {
            _globalSettings.Setup = false;
            _globalSettings.PiDevices = null;
            _dispatcher.SetGlobalSettings(_globalSettings);
            _dispatcher.SendToPropertyInspector(LastContext, LastUUID, new { Update = true });
            return;
        }

        private void Setup()
        {
            _dispatcher.GetGlobalSettings();
            if (_globalSettings.ProjectId == null || _globalSettings.ClientId == null)
            {
                _logger.LogWarning("ProjectId or ClientId is not set in global settings. Can not start setup");
                return;
            }

            if (ListenerTask == null)
            {
                ListenerTask = ListenForCallback();
            }

            var url = NestService.GetAccountLinkUrl(_globalSettings.ProjectId, _globalSettings.ClientId, "http://localhost:20777");

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

        protected async Task ProcessRequest(HttpListenerContext _http, CancellationToken stoppingToken)
        {
            try
            {
                HttpListenerRequest request = _http.Request;

                var query = _http.Request.QueryString;
                var code = query["code"]?.ToString();
                var scope = query["scope"]?.ToString();
                string exception = "";
                try
                {
                    await _nestService.ConnectWithCode(_globalSettings.ProjectId!, _globalSettings.CloudProjectId!, _globalSettings.ClientId!, _globalSettings.ClientSecret!, "http://localhost:20777", code!, _cancellationTokenSource.Token);
                }
                catch (Exception e)
                {
                    exception = e.ToString();
                }
                var responseString = $"Error please check your settings \r\n" +
                    $"id:{_globalSettings.ClientId} partialsecret:{new string(_globalSettings.ClientSecret?.Take(5).ToArray())} projId: {_globalSettings.ProjectId} cloudproj{_globalSettings.CloudProjectId}\r\n" +
                    $"Code: {code} Scope: {scope}\r\nException: {exception}";

                if (_nestService.RefreshToken != null)
                {
                    _globalSettings.Code = code;
                    _globalSettings.Scope = scope;
                    _globalSettings.Setup = true;
                    _globalSettings.RefreshToken = _nestService.RefreshToken;
                    _globalSettings.SubscriptionId = _nestService.SubscriptionId;
                    _dispatcher.SetGlobalSettings(_globalSettings);
                    await Communication.MetricsAsync();
                    responseString = "Success! You can now close this window.";
                }
                else
                {
                    await Communication.LogAsync(LogLevel.Critical, responseString);
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
            catch (Exception e)
            {

                if (_http.Response.OutputStream.CanWrite)
                {
                    //Write Response
                    HttpListenerResponse response = _http.Response;
                    response.StatusCode = 200;
                    byte[] buffer = System.Text.Encoding.UTF8.GetBytes(e.ToString());
                    response.ContentLength64 = buffer.Length;
                    System.IO.Stream output = response.OutputStream;
                    await output.WriteAsync(buffer, 0, buffer.Length);
                    output.Close();
                }
            }

        }


        public async ValueTask DisposeAsync()
        {

        }
    }
}
