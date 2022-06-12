using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Tech.Aerove.StreamDeck.NestControl.Tech.Aerove.Tools.Nest.Models.WebCalls;
using Tech.Aerove.Tools.Nest.Models.WebCalls;

namespace Tech.Aerove.Tools.Nest
{
    internal static class WebCalls
    {
        /// <summary>
        /// Url to send the user to in order to link an account
        /// </summary>
        /// <returns></returns>
        public static string GetAccountLinkUrl(string projectId, string clientId, string redirectUrl)
        {
            var url = $"https://nestservices.google.com/partnerconnections/{projectId}/auth" +
            $"?redirect_uri={redirectUrl}" +
            $"&access_type=offline" +
            $"&prompt=consent" +
            $"&client_id={clientId}" +
            $"&response_type=code" +
            $"&scope=https://www.googleapis.com/auth/sdm.service";
            return url;
        }

        /// <summary>
        /// Called with the code opened from linking an account
        /// </summary>
        /// <returns></returns>
        public static AccessTokenResponse? GetFirstAccessToken(string clientId, string clientSecret, string redirectUrl, string code)
        {
            var client = new RestClient($"https://www.googleapis.com/oauth2/v4/token" +
              $"?client_id={clientId}" +
              $"&client_secret={clientSecret}" +
              $"&code={code}" +
              $"&grant_type=authorization_code" +
              $"&redirect_uri={redirectUrl}");
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
            catch (Exception e)
            {
                return null;
            }
        }

        //https://developers.google.com/nest/device-access/traits/device/thermostat-temperature-setpoint#setcool
        public static bool ExecuteCommand(string deviceName, string accessToken, CommandBody command)
        {
            var client = new RestClient($"https://smartdevicemanagement.googleapis.com/v1/{deviceName}:executeCommand");
            var request = new RestRequest(Method.POST);
            request.AddHeader("Content-Type", "application/json");
            request.AddHeader("Authorization", $"Bearer {accessToken}");


            request.AddParameter("application/json", JsonConvert.SerializeObject(command), ParameterType.RequestBody);
            IRestResponse response = client.Execute(request);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                return true;
            }
            return false;
        }
    }
}
