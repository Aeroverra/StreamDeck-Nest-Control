using Tech.Aerove.StreamDeck.NestControl.Tech.Aerove.Tools.Nest.Models.WebCalls;
using Tech.Aerove.Tools.Nest.Models;
using Tech.Aerove.Tools.Nest.Models.WebCalls;

namespace Tech.Aerove.Tools.Nest
{
    public class NestClient
    {

        private readonly PubSubClient PubSubClient;
        private readonly string ClientId;
        private readonly string ClientSecret;
        private readonly string ProjectId;
        private readonly string CloudProjectId;
        private string AccessToken { get; set; } = "";
        private string RefreshToken { get; set; } = "";
        public List<string> Scopes { get; private set; } = new List<string>();
        private DateTime AccessTokenExpireTime = DateTime.MinValue;
        internal DevicesResponse DevicesResponse { get; set; } = new DevicesResponse();
        private List<ThermostatDevice> ThermostatDevices = new List<ThermostatDevice>();
        private static SemaphoreSlim Lock = new SemaphoreSlim(1);
        private readonly Timer timer;
        public string GetAccessToken()
        {
            var t = GetAccessToken;
            CheckUpdateToken();
            return AccessToken;
        }

        /// <summary>
        /// create client that needs to be setup by calling the getaccountlinkurl then finishsetup functions
        /// </summary>
        public NestClient(string clientId, string clientSecret, string projectId, string cloudProjectId, Action<string> saveSubscriptionId)
        {
            timer = new Timer(TimerStartTask);
            ClientId = clientId;
            ClientSecret = clientSecret;
            ProjectId = projectId;
            CloudProjectId = cloudProjectId;
            PubSubClient = new PubSubClient(CloudProjectId, ProjectId, saveSubscriptionId, GetAccessToken);
            PubSubClient.OnDeviceUpdated += OnDeviceUpdated;
        }

        /// <summary>
        /// create already setup client
        /// </summary>
        public NestClient(string clientId, string clientSecret, string projectId, string refreshToken, string scopes, string cloudProjectId, string subscriptionId, Action<string> saveSubscriptionId)
        {
            timer = new Timer(TimerStartTask);
            ClientId = clientId;
            ClientSecret = clientSecret;
            ProjectId = projectId;
            RefreshToken = refreshToken;
            Scopes = scopes.Split(" ").ToList();
            CloudProjectId = cloudProjectId;
            PubSubClient = new PubSubClient(subscriptionId, CloudProjectId, ProjectId, saveSubscriptionId, GetAccessToken);
            PubSubClient.OnDeviceUpdated += OnDeviceUpdated;
            UpdateDevices(true);
            _ = PubSubClient.Start(Scopes);
        }

        public event EventHandler<string> OnDevicesUpdated;

        private void OnDeviceUpdated(object? sender, Device device)
        {
            var existingDevice = DevicesResponse.Devices.SingleOrDefault(x => x.Name == device.Name);
            if (existingDevice == null)
            {
                return;
            }
            if (device.Traits.SdmDevicesTraitsInfo != null)
            {
                existingDevice.Traits.SdmDevicesTraitsInfo = device.Traits.SdmDevicesTraitsInfo;
            }

            if (device.Traits.SdmDevicesTraitsHumidity != null)
            {
                existingDevice.Traits.SdmDevicesTraitsHumidity = device.Traits.SdmDevicesTraitsHumidity;
            }

            if (device.Traits.SdmDevicesTraitsConnectivity != null)
            {
                existingDevice.Traits.SdmDevicesTraitsConnectivity = device.Traits.SdmDevicesTraitsConnectivity;
            }

            if (device.Traits.SdmDevicesTraitsFan != null)
            {
                existingDevice.Traits.SdmDevicesTraitsFan = device.Traits.SdmDevicesTraitsFan;
            }

            if (device.Traits.SdmDevicesTraitsThermostatMode != null)
            {
                existingDevice.Traits.SdmDevicesTraitsThermostatMode = device.Traits.SdmDevicesTraitsThermostatMode;
            }

            if (device.Traits.SdmDevicesTraitsThermostatEco != null)
            {
                existingDevice.Traits.SdmDevicesTraitsThermostatEco = device.Traits.SdmDevicesTraitsThermostatEco;
            }

            if (device.Traits.SdmDevicesTraitsThermostatHvac != null)
            {
                existingDevice.Traits.SdmDevicesTraitsThermostatHvac = device.Traits.SdmDevicesTraitsThermostatHvac;
            }

            if (device.Traits.SdmDevicesTraitsSettings != null)
            {
                existingDevice.Traits.SdmDevicesTraitsSettings = device.Traits.SdmDevicesTraitsSettings;
            }

            if (device.Traits.SdmDevicesTraitsThermostatTemperatureSetpoint != null)
            {
                existingDevice.Traits.SdmDevicesTraitsThermostatTemperatureSetpoint = device.Traits.SdmDevicesTraitsThermostatTemperatureSetpoint;

            }
            if (device.Traits.SdmDevicesTraitsTemperature != null)
            {
                existingDevice.Traits.SdmDevicesTraitsTemperature = device.Traits.SdmDevicesTraitsTemperature;
            }

            OnDevicesUpdated?.Invoke(this, device.Name);
        }

        private string RedirectUrl = "";

        /// <summary>
        /// Send the user to this url to link their account
        /// </summary>
        /// <param name="redirectUrl">The webserver it should redirect the user to after. This server should
        /// read the code from the client url param and then call the finishsetup function</param>
        /// <returns></returns>
        public string GetAccountLinkUrl(string redirectUrl)
        {
            RedirectUrl = redirectUrl;
            return WebCalls.GetAccountLinkUrl(ProjectId, ClientId, redirectUrl);
        }

        /// <summary>
        /// Retrieves the auth token and refresh token needed to make future calls
        /// </summary>
        /// <param name="code">Retrieved from the webserver url param specified in the redirecturl
        /// of the function GetAccountLinkUrl</param>
        /// <returns>refresh token if success</returns>
        public string? FinishSetup(string code, string scope)
        {
            Scopes = scope.Split(" ").ToList();
            var response = WebCalls.GetFirstAccessToken(ClientId, ClientSecret, RedirectUrl, code);
            if (response == null) { return null; }
            RefreshToken = response.RefreshToken;
            AccessToken = response.AccessToken;
            AccessTokenExpireTime = DateTime.Now.AddSeconds(response.ExpiresIn - 10);
            var ts = AccessTokenExpireTime.AddSeconds(10) - DateTime.Now;
            timer.Change(Convert.ToInt64(ts.TotalMilliseconds), Timeout.Infinite);
            if (!UpdateDevices()) { return null; }
            _ = PubSubClient.Start(Scopes);
            return RefreshToken;
        }


        private readonly SemaphoreSlim GetLock = new SemaphoreSlim(1);
        public ThermostatDevice GetThermostat(string name)
        {
            GetLock.Wait();
            try
            {
                var thermostatDevice = ThermostatDevices.FirstOrDefault(x => x.Name == name);
                if (thermostatDevice != null)
                {
                    return thermostatDevice;
                }
                var device = DevicesResponse.Devices.FirstOrDefault(x => x.Name.ToLower() == name.ToLower());
                thermostatDevice = new ThermostatDevice(this, device.Name);
                ThermostatDevices.Add(thermostatDevice);
                return thermostatDevice;
            }
            finally
            {
                GetLock.Release();
            }
        }

        public List<ThermostatDevice> GetThermostats()
        {
            foreach (var device in DevicesResponse.Devices.Where(x => x.Type == "sdm.devices.types.THERMOSTAT"))
            {
                if (ThermostatDevices.Any(x => x.Name == device.Name))
                {
                    continue;
                }
                ThermostatDevices.Add(new ThermostatDevice(this, device.Name));
            }
            return ThermostatDevices;
        }

        private bool UpdateDevices(bool skipPubSub = false)
        {

            //used for constructor call to prevent two pubsubclients
            CheckUpdateToken(skipPubSub);

            var newDevicesResponse = WebCalls.GetDevices(ProjectId, AccessToken);
            if (DevicesResponse == null) { return false; }
            DevicesResponse.Update(newDevicesResponse);
            return true;
        }

       
        private void TimerStartTask(object state)
        {
            CheckUpdateToken();
        }
    
        private void CheckUpdateToken(bool skippubsub = false)
        {

            if (AccessTokenExpireTime > DateTime.Now)
            {
                return;
            }
            //System.Exception: 'Failed to refresh token!'

            var response = WebCalls.RefreshToken(ClientId, ClientSecret, RefreshToken);
            if (response == null) { throw new Exception("Failed to refresh token!"); }
            AccessTokenExpireTime = DateTime.Now.AddSeconds(response.ExpiresIn - 10);
            AccessToken = response.AccessToken;
            var ts = AccessTokenExpireTime.AddSeconds(10) - DateTime.Now;
            timer.Change(Convert.ToInt64(ts.TotalMilliseconds), Timeout.Infinite);
            if (!skippubsub)
            {
                _ = PubSubClient.Start(Scopes);
            }
       
        }

        public bool SetMode(ThermostatDevice thermostat, ThermostatMode mode)
        {
            CheckUpdateToken();
            var command = new CommandBody
            {
                Command = "sdm.devices.commands.ThermostatMode.SetMode",
            };
            command.Params.Add("mode", $"{mode}");
            var success = WebCalls.ExecuteCommand(thermostat.Name, AccessToken, command);
            if (success)
            {
                var device = DevicesResponse.Devices.FirstOrDefault(x => x.Name == thermostat.Name);
                device.SetMode(mode);
            }
            return success;
        }

        public bool SetTemp(ThermostatDevice thermostat, decimal value)
        {
            CheckUpdateToken();

            //api always takes celsius even if thermostat is not
            if (thermostat.Scale == TemperatureScale.FAHRENHEIT)
            {
                value = value.ToCelsius();
            }

            if (thermostat.Mode != ThermostatMode.HEAT && thermostat.Mode != ThermostatMode.COOL) { return false; }

            var command = new CommandBody();
            if (thermostat.Mode == ThermostatMode.COOL)
            {
                command.Command = "sdm.devices.commands.ThermostatTemperatureSetpoint.SetCool";
                command.Params.Add("coolCelsius", value);
            }
            else
            {
                command.Command = "sdm.devices.commands.ThermostatTemperatureSetpoint.SetHeat";
                command.Params.Add("heatCelsius", value);
            }
            var success = WebCalls.ExecuteCommand(thermostat.Name, AccessToken, command);
            if (success)
            {
                var device = DevicesResponse.Devices.FirstOrDefault(x => x.Name == thermostat.Name);
                device.SetTemperatureSetPoint(thermostat.Mode, value);
            }
            return success;
        }

        public bool SetTempUp(ThermostatDevice thermostat, int value)
        {
            var currentValue = thermostat.SetPointExact;
            currentValue += value;
            return SetTemp(thermostat, currentValue);
        }

        public bool SetTempDown(ThermostatDevice thermostat, int value)
        {
            var currentValue = thermostat.SetPointExact;
            currentValue -= value;
            return SetTemp(thermostat, currentValue);
        }
    }
}
