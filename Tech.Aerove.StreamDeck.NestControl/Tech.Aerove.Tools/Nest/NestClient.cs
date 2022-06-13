using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Cloud.PubSub.V1;
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
        private readonly string SubscriptionId = "A" + Guid.NewGuid().ToString();
        private readonly string ClientId;
        private readonly string ClientSecret;
        private readonly string ProjectId;
        private string AccessToken { get; set; } = "";
        private string RefreshToken { get; set; } = "";
        private List<string> Scopes { get; set; } = new List<string>();
        private DateTime AccessTokenExpireTime = DateTime.MinValue;
        internal DevicesResponse DevicesResponse { get; set; } = new DevicesResponse();

        private static SemaphoreSlim Lock = new SemaphoreSlim(1);


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
        public NestClient(string clientId, string clientSecret, string projectId, string refreshToken, string scopes)
        {
            ClientId = clientId;
            ClientSecret = clientSecret;
            ProjectId = projectId;
            RefreshToken = refreshToken;
            Scopes = scopes.Split(" ").ToList();
            UpdateDevices();
            _ = EventHandler();
        }

        public async Task EventHandler()
        {
            if (!Scopes.Contains("https://www.googleapis.com/auth/pubsub"))
            {
                return;
            }
            string credential_path = @"PATH TO GOOGLE AUTH CREDENTIAL JSON FILE";
            //System.Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", credential_path);
            try
            {


                var token = new TokenResponse
                {
                    RefreshToken = RefreshToken,
                    Scope = "https://www.googleapis.com/auth/sdm.service https://www.googleapis.com/auth/pubsub",
                    AccessToken = AccessToken,
                    IssuedUtc = DateTime.UtcNow,
                    ExpiresInSeconds = 900
                };
                var secrets = new ClientSecrets
                {
                    ClientId = ClientId,
                    ClientSecret = ClientSecret
                };
                var i = new GoogleAuthorizationCodeFlow.Initializer
                {
                    ClientSecrets = secrets,
                    ProjectId = "aeroveprod",
                    Scopes = new[] { "https://www.googleapis.com/auth/sdm.service", "https://www.googleapis.com/auth/pubsub" },
                };
                var cf = new GoogleAuthorizationCodeFlow(i);

                ICredential googleCredentials = new UserCredential(cf, "", token);

                //https://www.googleapis.com/auth/sdm.service
                //https://www.googleapis.com/auth/pubsub
                //https://www.googleapis.com/auth/cloud-platform
               googleCredentials = GoogleCredential
                   .FromAccessToken(AccessToken)
                   .CreateScoped("https://www.googleapis.com/auth/sdm.service", "https://www.googleapis.com/auth/pubsub");

                SubscriberServiceApiClientBuilder builder = new SubscriberServiceApiClientBuilder();
                builder.Credential = googleCredentials;
                var subscriberService = builder.Build();



                //SubscriberServiceApiClient subscriberService = await SubscriberServiceApiClient.CreateAsync();
                SubscriptionName subscriptionName = new SubscriptionName("aeroveprod", SubscriptionId);


                // Subscribe to the topic.
                TopicName topicName = new TopicName("sdm-prod", $"enterprise-{ProjectId}");
                subscriberService.CreateSubscription(subscriptionName, topicName, pushConfig: null, ackDeadlineSeconds: 60);

                // Pull messages from the subscription using SubscriberClient.
                SubscriberClient subscriber = await SubscriberClient.CreateAsync(subscriptionName);
                List<PubsubMessage> receivedMessages = new List<PubsubMessage>();
                // Start the subscriber listening for messages.
                await subscriber.StartAsync((msg, cancellationToken) =>
                {
                    receivedMessages.Add(msg);
                    Console.WriteLine($"Received message {msg.MessageId} published at {msg.PublishTime.ToDateTime()}");
                    Console.WriteLine($"Text: '{msg.Data.ToStringUtf8()}'");
                    // Stop this subscriber after one message is received.
                    // This is non-blocking, and the returned Task may be awaited.
                    subscriber.StopAsync(TimeSpan.FromSeconds(15));
                    // Return Reply.Ack to indicate this message has been handled.
                    return Task.FromResult(SubscriberClient.Reply.Ack);
                });
                subscriberService.DeleteSubscription(subscriptionName);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
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
            if (!UpdateDevices()) { return null; }
            _ = EventHandler();
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
            foreach (var device in DevicesResponse.Devices.Where(x => x.Type == "sdm.devices.types.THERMOSTAT"))
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
            //System.Exception: 'Failed to refresh token!'

            var response = WebCalls.RefreshToken(ClientId, ClientSecret, RefreshToken);
            if (response == null) { throw new Exception("Failed to refresh token!"); }
            AccessTokenExpireTime = DateTime.Now.AddSeconds(response.ExpiresIn - 10);
            AccessToken = response.AccessToken;
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
