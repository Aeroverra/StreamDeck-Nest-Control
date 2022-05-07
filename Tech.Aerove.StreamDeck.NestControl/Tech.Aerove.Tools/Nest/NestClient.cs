using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tech.Aerove.StreamDeck.NestControl.Tech.Aerove.Tools.Nest.Models;
using Tech.Aerove.StreamDeck.NestControl.Tech.Aerove.Tools.Nest.Models.WebCalls;
using Tech.Aerove.Tools.Nest.Models;
using Tech.Aerove.Tools.Nest.Models.WebCalls;

namespace Tech.Aerove.Tools.Nest
{
    public class NestClient
    {
        private readonly string ClientId;
        private readonly string ClientSecret;
        private readonly string ProjectId;
        private string AccessToken { get; set; } = "";
        private string RefreshToken { get; set; } = "";
        private DateTime AccessTokenExpireTime = DateTime.MinValue;
        internal DevicesResponse DevicesResponse { get; set; } = new DevicesResponse();

        /// <summary>
        /// create client that needs to be setup by calling the getaccountlinkurl then finishsetup functions
        /// </summary>
        public NestClient(string clientId, string clientSecret, string projectId)
        {
            ClientId = clientId;
            ClientSecret = clientSecret;
            ProjectId = projectId;
        }

        /// <summary>
        /// create already setup client
        /// </summary>
        public NestClient(string clientId, string clientSecret, string projectId, string refreshToken)
        {
            ClientId = clientId;
            ClientSecret = clientSecret;
            ProjectId = projectId;
            RefreshToken = refreshToken;
            UpdateDevices();

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
        public string? FinishSetup(string code)
        {
            var response = WebCalls.GetFirstAccessToken(ClientId, ClientSecret, RedirectUrl, code);
            if (response == null) { return null; }
            RefreshToken = response.RefreshToken;
            AccessToken = response.AccessToken;
            AccessTokenExpireTime = DateTime.Now.AddSeconds(response.ExpiresIn - 10);
            if (!UpdateDevices()) { return null; }
            return RefreshToken;
        }

        public ThermostatDevice GetThermostat(string name)
        {
            var device = DevicesResponse.Devices.FirstOrDefault(x => x.Name.ToLower() == name.ToLower());
            var thermostatDevice = new ThermostatDevice(this, device.Name);
            return thermostatDevice;
        }

        public List<ThermostatDevice> GetThermostats()
        {
            var thermostats = new List<ThermostatDevice>();
            foreach (var device in DevicesResponse.Devices)
            {
                thermostats.Add(new ThermostatDevice(this, device.Name));
            }
            return thermostats;
        }

        private bool UpdateDevices()
        {
            CheckUpdateToken();
            DevicesResponse = WebCalls.GetDevices(ProjectId, AccessToken);
            if (DevicesResponse == null) { return false; }
            return true;
        }

        private void CheckUpdateToken()
        {
            if (AccessTokenExpireTime > DateTime.Now)
            {
                return;
            }
            var response = WebCalls.RefreshToken(ClientId, ClientSecret, RefreshToken);
            if (response == null) { throw new Exception("Failed to refresh token!"); }
            AccessTokenExpireTime = DateTime.Now.AddSeconds(response.ExpiresIn - 10);
            AccessToken = response.AccessToken;
        }

        public bool SetMode(string deviceName, ThermostatMode mode)
        {
            CheckUpdateToken();
            var command = new CommandBody
            {
                Command = "sdm.devices.commands.ThermostatMode.SetMode",
            };
            command.Params.Add("mode", $"{mode}");
            var success = WebCalls.ExecuteCommand(deviceName, AccessToken, command);
            return success;
        }

        public bool SetTemp(string deviceName, decimal value)
        {
            CheckUpdateToken();
            var device = DevicesResponse.GetDevice(deviceName);

            //api always takes celsius even if thermostat is not
            if (device.GetTemperatureScale() == TemperatureScale.FAHRENHEIT)
            {
                value = value.ToCelsius();
            }


            var mode = device.GetMode();
            if (mode != (ThermostatMode.HEAT | ThermostatMode.COOL)) { return false; }

            var command = new CommandBody();
            if (mode == ThermostatMode.COOL)
            {
                command.Command = "sdm.devices.commands.ThermostatTemperatureSetpoint.SetCool";
                command.Params.Add("coolCelsius", value);
            }
            else
            {
                command.Command = "sdm.devices.commands.ThermostatTemperatureSetpoint.SetHeat";
                command.Params.Add("heatCelsius", value);
            }
            var success = WebCalls.ExecuteCommand(deviceName, AccessToken, command);
            return success;
        }

        public bool SetTempUp(string deviceName, int value)
        {
            var device = DevicesResponse.GetDevice(deviceName);
            var currentValue = device.GetTemperatureSetPoint();
            if (device.GetTemperatureScale() == TemperatureScale.FAHRENHEIT)
            {
                currentValue = currentValue.ToFahrenheit();
                currentValue += value;
                currentValue = currentValue.ToCelsius();
            }
            else
            {
                currentValue += value;
            }
            return SetTemp(deviceName, currentValue);
        }

        public bool SetTempDown(string deviceName, int value)
        {
            var device = DevicesResponse.GetDevice(deviceName);
            var currentValue = device.GetTemperatureSetPoint();
            if (device.GetTemperatureScale() == TemperatureScale.FAHRENHEIT)
            {
                currentValue = currentValue.ToFahrenheit();
                currentValue -= value;
                currentValue = currentValue.ToCelsius();
            }
            else
            {
                currentValue -= value;
            }
            return SetTemp(deviceName, currentValue);
        }
    }
}
