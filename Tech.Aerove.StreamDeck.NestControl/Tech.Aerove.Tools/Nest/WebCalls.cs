using Aeroverra.StreamDeck.NestControl;
using Aeroverra.StreamDeck.NestControl.Tech.Aerove.Tools.Nest.Models.WebCalls;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Services;
using Google.Apis.SmartDeviceManagement.v1;
using Google.Apis.SmartDeviceManagement.v1.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System.Collections.Generic;
using System.Threading;
using Tech.Aerove.Tools.Nest.Models.WebCalls;

namespace Tech.Aerove.Tools.Nest
{
    internal static class WebCalls
    {
        private static SmartDeviceManagementService CreateService(string accessToken)
        {
            var credentials = GoogleCredential.FromAccessToken(accessToken);
            return new SmartDeviceManagementService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credentials,
                ApplicationName = "Aerove Stream Deck Nest Control"
            });
        }

        /// <summary>
        /// Url to send the user to in order to link an account
        /// </summary>
        /// <returns></returns>
        public static string GetAccountLinkUrl(string projectId, string clientId, string redirectUrl)
        {
            var scopes = $"{SmartDeviceManagementService.ScopeConstants.SdmService} https://www.googleapis.com/auth/pubsub";
            var encodedScopes = Uri.EscapeDataString(scopes);
            var url = $"https://nestservices.google.com/partnerconnections/{projectId}/auth" +
            $"?redirect_uri={redirectUrl}" +
            $"&access_type=offline" +
            $"&prompt=consent" +
            $"&client_id={clientId}" +
            $"&response_type=code" +
            $"&scope={encodedScopes}";
            return url;
        }

        /// <summary>
        /// Called with the code opened from linking an account
        /// </summary>
        /// <returns></returns>
        public static AccessTokenResponse? GetFirstAccessToken(string clientId, string clientSecret, string redirectUrl, string code)
        {
            try
            {
                var scopes = new[]
                {
                    SmartDeviceManagementService.ScopeConstants.SdmService,
                    "https://www.googleapis.com/auth/pubsub"
                };

                var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
                {
                    ClientSecrets = new ClientSecrets { ClientId = clientId, ClientSecret = clientSecret },
                    Scopes = scopes
                });

                TokenResponse token = flow
                    .ExchangeCodeForTokenAsync("user", code, redirectUrl, CancellationToken.None)
                    .GetAwaiter()
                    .GetResult();

                if (string.IsNullOrWhiteSpace(token.AccessToken))
                {
                    throw new Exception("Google returned an empty access token when exchanging the authorization code.");
                }

                return new AccessTokenResponse
                {
                    AccessToken = token.AccessToken,
                    ExpiresIn = token.ExpiresInSeconds ?? 0,
                    RefreshToken = token.RefreshToken,
                    Scope = string.IsNullOrWhiteSpace(token.Scope) ? null : new Uri(token.Scope),
                    TokenType = token.TokenType
                };
            }
            catch (Exception e)
            {

                throw e;
            }
        }


        public static DevicesResponse? GetDevices(string projectId, string accessToken)
        {
            try
            {
                using var service = CreateService(accessToken);
                var response = service.Enterprises.Devices.List($"enterprises/{projectId}").Execute();
                if (response?.Devices == null)
                {
                    var msg = "Google did not return devices (empty response).";
                    _ = Communication.LogAsync(LogLevel.Critical, msg);
                    throw new Exception(msg);
                }

                var parsedResponse = JsonConvert.DeserializeObject<DevicesResponse>(JsonConvert.SerializeObject(response));
                if (parsedResponse?.Devices == null)
                {
                    var msg = "Google returned devices but parsing failed.";
                    _ = Communication.LogAsync(LogLevel.Critical, msg);
                    throw new Exception(msg);
                }
                return parsedResponse;
            }
            catch (Exception e)
            {
                var msg = $"Google did not return devices\r\n{e}";
                _ = Communication.LogAsync(LogLevel.Critical, msg);
                throw;
            }
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
            using var service = CreateService(accessToken);
            try
            {
                var requestBody = new GoogleHomeEnterpriseSdmV1ExecuteDeviceCommandRequest
                {
                    Command = command.Command,
                    Params__ = command.Params?.ToObject<Dictionary<string, object>>() ?? new Dictionary<string, object>()
                };
                var request = service.Enterprises.Devices.ExecuteCommand(requestBody, deviceName);
                _ = request.Execute();
                return true;
            }
            catch (Exception e)
            {
                _ = Communication.LogAsync(LogLevel.Critical, $"ExecuteCommand failed for {deviceName}\r\n{e}");
                return false;
            }
        }
    }
}
