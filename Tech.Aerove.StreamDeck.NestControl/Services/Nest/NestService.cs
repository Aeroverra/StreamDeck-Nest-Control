using Aeroverra.StreamDeck.NestControl.Services.Nest.Models;
using Google.Api.Gax.ResourceNames;
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
    public class NestService(ILogger<NestService> logger)
    {

        public event EventHandler<GoogleHomeEnterpriseSdmV1Device>? OnDeviceUpdated;
        public event EventHandler? OnConnected;

        public IReadOnlyList<GoogleHomeEnterpriseSdmV1Device> Devices => DeviceDictionary.Values.ToList();

        public string? ProjectId { get; private set; }
        public string? RefreshToken => _credentials?.Token.RefreshToken;
        public string? SubscriptionId { get; private set; }
        public string? CloudProjectId { get; private set; }

        private SubscriberClient? SubscriberClient = null;

        private Dictionary<string, GoogleHomeEnterpriseSdmV1Device> DeviceDictionary = new Dictionary<string, GoogleHomeEnterpriseSdmV1Device>();

        /// <summary>
        /// Google handles automatic refresh and the refresh token does not change
        /// </summary>
        private UserCredential? _credentials;

        public async Task ConnectWithCode(string projectId, string cloudProjectId, string clientId, string clientSecret, string redirectUrl, string code, CancellationToken cancellationToken)
        {
            try
            {
                _credentials = await GetToken(clientId, clientSecret, null, code, redirectUrl, cancellationToken);
                ProjectId = projectId;
                await ConnectPrivate(cancellationToken);
            }
            catch (TaskCanceledException) { }
        }

        public async Task ConnectWithRefreshToken(string projectId, string cloudProjectId, string clientId, string clientSecret, string refreshToken, string subscriptionId, CancellationToken cancellationToken)
        {
            try
            {
                _credentials = await GetToken(clientId, clientSecret, refreshToken, null, null, cancellationToken);
                ProjectId = projectId;
                CloudProjectId = cloudProjectId;
                SubscriptionId = subscriptionId;
                await ConnectPrivate(cancellationToken);
            }
            catch (TaskCanceledException) { }
        }

        private async Task ConnectPrivate(CancellationToken cancellationToken)
        {
            try
            {
                var service = new SmartDeviceManagementService(new BaseClientService.Initializer
                {
                    HttpClientInitializer = _credentials,
                    ApplicationName = "Aeroverra Stream Deck Nest Control"
                });

                GoogleHomeEnterpriseSdmV1ListDevicesResponse response = await service.Enterprises.Devices.List($"enterprises/{ProjectId}").ExecuteAsync(cancellationToken);

                if (response?.Devices == null)
                {
                    var msg = "Google did not return devices (empty response).";
                    _ = Communication.LogAsync(LogLevel.Critical, msg);
                    throw new Exception(msg);
                }

                DeviceDictionary = response.Devices.ToDictionary(x => x.Name!, x => x);
                var t = response.Devices.First();
                //all the bs i had to look up and sort through (minus the useless stuff) to put this together
                //along with a little guessing because
                //google has no documentation on oauth pubsub C#
                //https://console.cloud.google.com/home/dashboard
                //https://grpc.github.io/grpc/csharp/api/Grpc.Auth.GoogleGrpcCredentials.html
                //https://stackoverflow.com/questions/71437035/google-googleapiexception-google-apis-requests-requesterror-request-had-insuff
                //https://stackoverflow.com/questions/45806451/authenticate-for-google-cloud-pubsub-using-parameters-from-a-config-file-in-c-n

                SubscriptionName subscriptionName;

                if (SubscriptionId == null)
                {
                    // Create a PublisherServiceApiClient to list topics
                    PublisherServiceApiClientBuilder pubBuilder = new PublisherServiceApiClientBuilder
                    {
                        ChannelCredentials = _credentials?.ToChannelCredentials()
                    };
                    PublisherServiceApiClient pubClient = await pubBuilder.BuildAsync(cancellationToken);

                    // List topics in the project
                    var topics = pubClient.ListTopics(new ProjectName(CloudProjectId!));

                    if(topics.Count() > 1)
                        throw new Exception($"User has more than 1 Pub/Sub topic");

                    var topic = topics.FirstOrDefault();

                    if (topic == null)
                        throw new Exception($"No Pub/Sub topics found in project {CloudProjectId}. Please create a topic for Nest Device Access.");


                    SubscriptionId = $"Aeroverra_StreamDeck";
                    subscriptionName = new SubscriptionName(CloudProjectId, SubscriptionId);

                    SubscriberServiceApiClientBuilder builder = new SubscriberServiceApiClientBuilder()
                    {
                        ChannelCredentials = _credentials?.ToChannelCredentials()
                    };
                    SubscriberServiceApiClient subscriberService = await builder.BuildAsync(cancellationToken);

                    Subscription? existingSubscription = null;
                    try
                    {
                        existingSubscription = await subscriberService.GetSubscriptionAsync(subscriptionName, cancellationToken);
                    }
                    catch (Grpc.Core.RpcException e)
                    {
                        if (e.Message.Contains("Resource not found") == false)
                            throw;
                    }

                    if (existingSubscription == null)
                    {
                        existingSubscription = await subscriberService.CreateSubscriptionAsync(subscriptionName, topic.TopicName, pushConfig: null, ackDeadlineSeconds: 60, cancellationToken);
                    }
                }
                else
                {
                    subscriptionName = new SubscriptionName(CloudProjectId, SubscriptionId);
                }

                SubscriberClientBuilder clientBuilder = new SubscriberClientBuilder()
                {
                    ChannelCredentials = _credentials?.ToChannelCredentials(),
                    SubscriptionName = subscriptionName,
                };

                SubscriberClient = await clientBuilder.BuildAsync(cancellationToken);

                // Start the subscriber listening for messages.
                _=  SubscriberClient.StartAsync(OnRecievePubSubMessage);

                OnConnected?.Invoke(this, EventArgs.Empty);

                foreach(var device in DeviceDictionary.Values)
                {
                    OnDeviceUpdated?.Invoke(this, device);
                }

            }
            catch (Exception e)
            {
                var msg = $"Google did not return devices\r\n{e}";
                _ = Communication.LogAsync(LogLevel.Critical, msg);
                throw;
            }
        }

        public async Task StopAsync()
        {
            if (SubscriberClient != null)
            {
                await SubscriberClient.StopAsync(CancellationToken.None);
                SubscriberClient = null;
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
            try
            {
                var device = jobject["resourceUpdate"]!.ToObject<GoogleHomeEnterpriseSdmV1Device>();

                if (DeviceDictionary.TryGetValue(device!.Name!, out var existingDevice))
                {
                    foreach (var trait in device.Traits)
                    {
                        existingDevice.Traits[trait.Key] = trait.Value;
                    }
                    OnDeviceUpdated?.Invoke(this, existingDevice);
                }
            }
            catch (Exception ex)
            {
                _ = Communication.LogAsync(LogLevel.Error, $"Failed to process Pub/Sub message:\r\n{ex}\r\nMessage Content:\r\n{content}");
                logger.LogError(ex, "Failed to process Pub/Sub message");
            }


            return Task.FromResult(SubscriberClient.Reply.Ack);
        }

        private async Task<UserCredential> GetToken(string clientId, string clientSecret, string? refreshToken, string? code, string? redirectUrl, CancellationToken cancellationToken)
        {
            var scopes = new[]
            {
                    SmartDeviceManagementService.ScopeConstants.SdmService,
                    "https://www.googleapis.com/auth/pubsub"
                };

            var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets { ClientId = clientId, ClientSecret = clientSecret },
                Scopes = scopes,
            });

            TokenResponse? token = null;
            if (code is not null)
            {
                token = await flow.ExchangeCodeForTokenAsync("user", code, redirectUrl, cancellationToken);
            }
            else
            {
                token = await flow.RefreshTokenAsync("user", refreshToken, cancellationToken);
            }


            if (string.IsNullOrWhiteSpace(token.AccessToken))
            {
                throw new Exception("Google returned an empty access token when exchanging the authorization code.");
            }

            var returnedScopes = token.Scope.Split(" ").ToList();
            if (!returnedScopes.Contains("https://www.googleapis.com/auth/pubsub"))
            {
                throw new Exception($"The required permission: 'https://www.googleapis.com/auth/pubsub' is not present.");
            }

            var credentials = new UserCredential(flow, "user", token);

            return credentials;

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

        public Task<bool> SetMode(GoogleHomeEnterpriseSdmV1Device thermostat, ThermostatMode mode)
        {
            var command = new CommandBody
            {
                Command = NestConstants.COMMAND_SET_MODE,
            };
            command.Params.Add(NestConstants.COMMAND_SET_MODE_PARAMETER, $"{mode}");
            var success = ExecuteCommand(thermostat.Name, command);
            return success;
        }

        public async Task<bool> SetTemp(GoogleHomeEnterpriseSdmV1Device thermostat, decimal heat, decimal cool)
        {
            var command = new CommandBody();
            var mode = thermostat.GetThermostatMode().Mode;
            if (mode == ThermostatMode.COOL)
            {
                command.Command = NestConstants.COMMAND_SET_COOLING_TEMPERATURE_PARAMETER;
                command.Params.Add(NestConstants.COMMAND_SET_COOLING_TEMPERATURE_PARAMETER, cool);
            }
            else if (mode == ThermostatMode.HEAT)
            {
                command.Command = NestConstants.COMMAND_SET_HEATING_TEMPERATURE;
                command.Params.Add(NestConstants.COMMAND_SET_HEATING_TEMPERATURE_PARAMETER, heat);
            }
            else if (mode == ThermostatMode.HEATCOOL)
            {
                command.Command = NestConstants.COMMAND_SET_RANGE_TEMPERATURE;
                command.Params.Add(NestConstants.COMMAND_SET_HEATING_TEMPERATURE_PARAMETER, heat);
                command.Params.Add(NestConstants.COMMAND_SET_COOLING_TEMPERATURE_PARAMETER, cool);
            }
            else
            {
                return false;
            }
            var success = await ExecuteCommand(thermostat.Name, command);
            return success;
        }

        private async Task<bool> ExecuteCommand(string deviceName, CommandBody command)
        {
            var service = new SmartDeviceManagementService(new BaseClientService.Initializer
            {
                HttpClientInitializer = _credentials,
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
                await request.ExecuteAsync();
                return true;
            }
            catch (Exception e)
            {
                _ = Communication.LogAsync(LogLevel.Critical, $"ExecuteCommand failed for {deviceName}\r\n{e}");
                logger.LogError(e, "ExecuteCommand failed for {deviceName}", deviceName);
                return false;
            }
            finally
            {
                service.Dispose();
            }
        }
    }
}
