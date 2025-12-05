using Aeroverra.StreamDeck.NestControl.Services.Nest.Models;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Services;
using Google.Apis.SmartDeviceManagement.v1;
using Google.Apis.SmartDeviceManagement.v1.Data;
using Google.Cloud.PubSub.V1;
using Grpc.Auth;
using Newtonsoft.Json.Linq;

namespace Aeroverra.StreamDeck.NestControl.Services.Nest
{
    public class NestService
    {

        public string? ProjectId { get; private set; }
        public string? AccessToken { get; private set; }
        public string? RefreshToken { get; private set; }
        public string? SubscriptionId { get; private set; }
        public string? CloudProjectId { get; private set; }
        public IReadOnlyList<string> Scopes { get; private set; } = new List<string>();
        private DateTime AccessTokenExpireTime = DateTime.MinValue;
        private SubscriberClient? SubscriberClient = null;
        public IReadOnlyList<GoogleHomeEnterpriseSdmV1Device> Devices => DeviceDictionary.Values.ToList();
        private Dictionary<string, GoogleHomeEnterpriseSdmV1Device> DeviceDictionary = new Dictionary<string, GoogleHomeEnterpriseSdmV1Device>();
        public event EventHandler<GoogleHomeEnterpriseSdmV1Device>? OnDeviceUpdated;
        public event EventHandler? OnSetupComplete;
        private TokenResponse? _token;

        public async Task ConnectWithCode(string projectId, string cloudProjectId, string clientId, string clientSecret, string redirectUrl, string code)
        {
            var token = await GetToken(clientId, clientSecret, null, code, redirectUrl);
            Scopes = token.Scope.Split(" ").ToList();
            if (!Scopes.Contains("https://www.googleapis.com/auth/pubsub"))
            {
                throw new Exception($"The required permission: 'https://www.googleapis.com/auth/pubsub' is not present.");
            }
            AccessToken = token.AccessToken;
            RefreshToken = token.RefreshToken;
            AccessTokenExpireTime = DateTime.Now.AddSeconds(token.ExpiresInSeconds!.Value - 10);
            ProjectId = projectId;
            await ConnectPrivate();
        }

        public async Task ConnectWithRefreshToken(string projectId, string cloudProjectId, string clientId, string clientSecret, string refreshToken, string subscriptionId)
        {
            _token = await GetToken(clientId, clientSecret, refreshToken, null, null);
            Scopes = _token.Scope.Split(" ").ToList();
            if (!Scopes.Contains("https://www.googleapis.com/auth/pubsub"))
            {
                throw new Exception($"The required permission: 'https://www.googleapis.com/auth/pubsub' is not present.");
            }
            AccessToken = _token.AccessToken;
            RefreshToken = _token.RefreshToken;
            AccessTokenExpireTime = DateTime.Now.AddSeconds(_token.ExpiresInSeconds!.Value - 10);
            ProjectId = projectId;
            CloudProjectId = cloudProjectId;
            SubscriptionId = subscriptionId;
            await ConnectPrivate();
        }

        private async Task ConnectPrivate()
        {
            try
            {
                var credentials = GoogleCredential.FromAccessToken(AccessToken);
                var service = new SmartDeviceManagementService(new BaseClientService.Initializer
                {
                    HttpClientInitializer = credentials,
                    ApplicationName = "Aerove Stream Deck Nest Control"
                });

                GoogleHomeEnterpriseSdmV1ListDevicesResponse response = await service.Enterprises.Devices.List($"enterprises/{ProjectId}").ExecuteAsync();
                if (response?.Devices == null)
                {
                    var msg = "Google did not return devices (empty response).";
                    _ = Communication.LogAsync(LogLevel.Critical, msg);
                    throw new Exception(msg);
                }
                DeviceDictionary  = response.Devices.ToDictionary(x => x.Name!, x => x);


                //all the bs i had to look up and sort through (minus the useless stuff) to put this together
                //along with a little guessing because
                //google has no documentation on oauth pubsub C#
                //https://console.cloud.google.com/home/dashboard
                //https://grpc.github.io/grpc/csharp/api/Grpc.Auth.GoogleGrpcCredentials.html
                //https://stackoverflow.com/questions/71437035/google-googleapiexception-google-apis-requests-requesterror-request-had-insuff
                //https://stackoverflow.com/questions/45806451/authenticate-for-google-cloud-pubsub-using-parameters-from-a-config-file-in-c-n


                GoogleCredential googleCredentials = GoogleCredential
                   .FromAccessToken(AccessToken)
                   .CreateScoped("https://www.googleapis.com/auth/sdm.service", "https://www.googleapis.com/auth/pubsub");

                SubscriptionName subscriptionName;

                if (SubscriptionId == null)
                {
                    SubscriptionId = $"AeroveStreamDeck{Guid.NewGuid()}";
                    subscriptionName = new SubscriptionName(CloudProjectId, SubscriptionId);

                    // Subscribe to the topic.
                    TopicName topicName = TopicName.Parse("#TODOGetFromUserOrAutomatically");
                    SubscriberServiceApiClientBuilder builder = new SubscriberServiceApiClientBuilder();
                    builder.Credential = googleCredentials;
                    SubscriberServiceApiClient subscriberService = builder.Build();
                    subscriberService.CreateSubscription(subscriptionName, topicName, pushConfig: null, ackDeadlineSeconds: 60);
                }
                else
                {
                    subscriptionName = new SubscriptionName(CloudProjectId, SubscriptionId);
                }

                // Pull messages from the subscription using SubscriberClient.
                var grpcCredentials = googleCredentials.ToChannelCredentials();
                var creationSettings = new SubscriberClient.ClientCreationSettings(credentials: grpcCredentials);

                SubscriberClient = await SubscriberClient.CreateAsync(subscriptionName, creationSettings);

                // Start the subscriber listening for messages.
                _=  SubscriberClient.StartAsync(OnRecievePubSubMessage);

                OnSetupComplete?.Invoke(this, EventArgs.Empty);

            }
            catch (Exception e)
            {
                var msg = $"Google did not return devices\r\n{e}";
                _ = Communication.LogAsync(LogLevel.Critical, msg);
                throw;
            }
        }

        private DateTime LastMsgPublish = DateTime.MinValue;
        private Task<SubscriberClient.Reply> OnRecievePubSubMessage(PubsubMessage msg, CancellationToken cancellationToken)
        {
            var publishedTime = msg.PublishTime.ToDateTime();
            var content = msg.Data.ToStringUtf8();
            if (publishedTime < LastMsgPublish)
            {
                return Task.FromResult(SubscriberClient.Reply.Ack);
            }
            LastMsgPublish = publishedTime;

            var jobject = JObject.Parse(content);
            var device = jobject["resourceUpdate"].ToObject<GoogleHomeEnterpriseSdmV1Device>();

            if (DeviceDictionary.TryGetValue(device.Name!, out var existingDevice))
            {
                foreach (var trait in device.Traits)
                {
                    existingDevice.Traits[trait.Key] = trait.Value;
                }
                OnDeviceUpdated?.Invoke(this, existingDevice);
            }


            return Task.FromResult(SubscriberClient.Reply.Ack);
        }

        private async Task<TokenResponse> GetToken(string clientId, string clientSecret, string? refreshToken, string? code, string? redirectUrl)
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

                TokenResponse? token = null;
                if (code is not null)
                {
                    token = await flow.ExchangeCodeForTokenAsync("user", code, redirectUrl, CancellationToken.None);
                }
                else
                {
                    token = await flow.RefreshTokenAsync("user", refreshToken, CancellationToken.None);
                }


                if (string.IsNullOrWhiteSpace(token.AccessToken))
                {
                    throw new Exception("Google returned an empty access token when exchanging the authorization code.");
                }

                return token;
            }
            catch (Exception)
            {
                throw;
            }
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

        public bool SetMode(GoogleHomeEnterpriseSdmV1Device thermostat, ThermostatMode mode)
        {
            var command = new CommandBody
            {
                Command = "sdm.devices.commands.ThermostatMode.SetMode",
            };
            command.Params.Add("mode", $"{mode}");
            var success = ExecuteCommand(thermostat.Name, command);
            return success;
        }

        public bool SetTemp(GoogleHomeEnterpriseSdmV1Device thermostat, decimal heat, decimal cool)
        {
            var command = new CommandBody();
            var mode = thermostat.Traits.GetTrait<ThermostatModeTrait>("sdm.devices.traits.ThermostatMode").Mode;
            if (mode == ThermostatMode.COOL)
            {
                command.Command = "sdm.devices.commands.ThermostatTemperatureSetpoint.SetCool";
                command.Params.Add("coolCelsius", cool);
            }
            else if (mode == ThermostatMode.HEAT)
            {
                command.Command = "sdm.devices.commands.ThermostatTemperatureSetpoint.SetHeat";
                command.Params.Add("heatCelsius", heat);
            }
            else if (mode == ThermostatMode.HEATCOOL)
            {
                command.Command = "sdm.devices.commands.ThermostatTemperatureSetpoint.SetRange";
                command.Params.Add("heatCelsius", heat);
                command.Params.Add("coolCelsius", cool);
            }
            else
            {
                return false;
            }
            var success = ExecuteCommand(thermostat.Name, command);
            return success;
        }

        private bool ExecuteCommand(string deviceName, CommandBody command)
        {
            var credentials = GoogleCredential.FromAccessToken(AccessToken);
            var service = new SmartDeviceManagementService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credentials,
                ApplicationName = "Aerove Stream Deck Nest Control"
            });
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
            finally
            {
                service.Dispose();
            }
        }
    }
}
