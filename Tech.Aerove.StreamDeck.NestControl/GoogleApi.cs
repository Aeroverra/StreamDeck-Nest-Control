using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Tech.Aerove.StreamDeck.NestControl.Models.GoogleApi;

namespace Tech.Aerove.StreamDeck.NestControl
{
    internal static class GoogleApi
    {
        public static string GetAccountLinkUrl(string projectId, string clientId)
        {
            var url = $"https://nestservices.google.com/partnerconnections/{projectId}/auth" +
            $"?redirect_uri=http://localhost:20777" +
            $"&access_type=offline" +
            $"&prompt=consent" +
            $"&client_id={clientId}" +
            $"&response_type=code" +
            $"&scope=https://www.googleapis.com/auth/sdm.service";
            return url;
        }

        public static AccessTokenResponse? GetAccessToken(string clientId, string clientSecret, string code)
        {
            var client = new RestClient($"https://www.googleapis.com/oauth2/v4/token" +
              $"?client_id={clientId}" +
              $"&client_secret={clientSecret}" +
              $"&code={code}" +
              $"&grant_type=authorization_code" +
              $"&redirect_uri=http://localhost:20777");
            var request = new RestRequest(Method.POST);
            IRestResponse response = client.Execute(request);
            try
            {
                var r = JObject.Parse(response.Content).ToObject<AccessTokenResponse>();
                if (String.IsNullOrWhiteSpace(r.AccessToken)) { return null; }
                return r;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static DevicesResponse? GetDevices(string projectId, string accessToken)
        {
            var client = new RestClient($"https://smartdevicemanagement.googleapis.com/v1/enterprises/{projectId}/devices");
            var request = new RestRequest(Method.GET);
            request.AddHeader("Content-Type", "application/json");
            request.AddHeader("Authorization", $"Bearer {accessToken}");
            IRestResponse response = client.Execute(request);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                return null;
            }
            return JObject.Parse(response.Content).ToObject<DevicesResponse>();
        }

        public static RefreshTokenResponse? RefreshToken(string clientId, string clientSecret, string refreshToken)
        {
            var client = new RestClient("https://www.googleapis.com/oauth2/v4/token" +
                $"?client_id={clientId}" +
                $"&client_secret={clientSecret}" +
                $"&refresh_token={refreshToken}" +
                $"&grant_type=refresh_token");

            var request = new RestRequest(Method.POST);
            IRestResponse response = client.Execute(request);
            try
            {
                var r = JObject.Parse(response.Content).ToObject<RefreshTokenResponse>();
                if (String.IsNullOrWhiteSpace(r.AccessToken)) { return null; }
                return r;
            }
            catch (Exception)
            {
                return null;
            }
        }


        public static bool SetTemp(Device device, string accessToken, string mode, decimal value)
        {
            string command = "";
            string paramName = "";
            switch (mode)
            {
                case "COOL":
                    command = "sdm.devices.commands.ThermostatTemperatureSetpoint.SetCool";
                    paramName = "coolCelsius";
                    break;
                case "HEAT":
                    command = "sdm.devices.commands.ThermostatTemperatureSetpoint.SetHeat";
                    paramName = "heatCelsius";
                    break;
            }
            var jBody = new JObject();
            jBody.Add("command", command);
            var param = new JObject();
            param.Add(paramName, value);
            jBody.Add("params", param);
            var success = ExecuteCommand(device, accessToken, jBody);
            return success;
        }

        public static bool SetMode(Device device, string accessToken, string mode)
        {
            var jBody = new JObject();
            jBody.Add("command", "sdm.devices.commands.ThermostatMode.SetMode");
            var param = new JObject();
            param.Add("mode", mode);
            jBody.Add("params", param);
            var success = ExecuteCommand(device, accessToken, jBody);
            return success;
        }

        private static bool ExecuteCommand(Device device, string accessToken, JObject jBody)
        {
            var client = new RestClient($"https://smartdevicemanagement.googleapis.com/v1/{device.Name}:executeCommand");
            var request = new RestRequest(Method.POST);
            request.AddHeader("Content-Type", "application/json");
            request.AddHeader("Authorization", $"Bearer {accessToken}");


            request.AddParameter("application/json", jBody.ToString(), ParameterType.RequestBody);
            IRestResponse response = client.Execute(request);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                return true;
            }
            return false;
        }

    }
}
