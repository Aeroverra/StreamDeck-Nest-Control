using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Cloud.PubSub.V1;
using Google.Protobuf.WellKnownTypes;
using Grpc.Auth;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
    internal class PubSubClient
    {
        public event EventHandler<Device> OnDeviceUpdated;

        private string SubscriptionId = null;
        private readonly string CloudProjectId;
        private readonly string ProjectId;
        private readonly Func<string> _getAccessToken;
        private readonly Action<string> _saveSubscriptionId;

        public PubSubClient(string cloudProjectId, string projectId, Action<string> saveSubscriptionId, Func<string> getAccessToken)
        {
            CloudProjectId = cloudProjectId;
            ProjectId = projectId;
            _saveSubscriptionId = saveSubscriptionId;
            _getAccessToken = getAccessToken;
        }

        public PubSubClient(string subscriptionId, string cloudProjectId, string projectId, Action<string> saveSubscriptionId, Func<string> getAccessToken)
        {
            SubscriptionId = subscriptionId;
            CloudProjectId = cloudProjectId;
            ProjectId = projectId;
            _saveSubscriptionId = saveSubscriptionId;
            _getAccessToken = getAccessToken;
        }

        private SubscriberClient SubscriberClient = null;

        public async Task Stop()
        {
            await SubscriberClient.StopAsync(TimeSpan.FromSeconds(10));
            SubscriberClient = null;
        }

        public async Task Start(List<string> Scopes)
        {
            if (SubscriberClient != null)
            {
                await Stop();
            }

            //all the bs i had to look up and sort through (minus the useless stuff) to put this together
            //along with a little guessing because
            //google has no documentation on oauth pubsub C#
            //https://console.cloud.google.com/home/dashboard
            //https://grpc.github.io/grpc/csharp/api/Grpc.Auth.GoogleGrpcCredentials.html
            //https://stackoverflow.com/questions/71437035/google-googleapiexception-google-apis-requests-requesterror-request-had-insuff
            //https://stackoverflow.com/questions/45806451/authenticate-for-google-cloud-pubsub-using-parameters-from-a-config-file-in-c-n
            if (!Scopes.Contains("https://www.googleapis.com/auth/pubsub"))
            {
                return;
            }
            try
            {

                GoogleCredential googleCredentials = GoogleCredential
                   .FromAccessToken(_getAccessToken())
                   .CreateScoped("https://www.googleapis.com/auth/sdm.service", "https://www.googleapis.com/auth/pubsub");

                SubscriptionName subscriptionName;

                if (SubscriptionId == null)
                {
                    SubscriptionId = $"AeroveStreamDeck{Guid.NewGuid()}";
                    subscriptionName = new SubscriptionName(CloudProjectId, SubscriptionId);

                    // Subscribe to the topic.
                    TopicName topicName = new TopicName("sdm-prod", $"enterprise-{ProjectId}");
                    SubscriberServiceApiClientBuilder builder = new SubscriberServiceApiClientBuilder();
                    builder.Credential = googleCredentials;
                    SubscriberServiceApiClient subscriberService = builder.Build();
                    subscriberService.CreateSubscription(subscriptionName, topicName, pushConfig: null, ackDeadlineSeconds: 60);
                    _saveSubscriptionId(SubscriptionId);
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
                _ = SubscriberClient.StartAsync(OnRecievePubSubMessage);

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
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
            Console.WriteLine($"Received message {msg.MessageId} published at {publishedTime}");

            var jobject = JObject.Parse(content);
            var device = jobject["resourceUpdate"].ToObject<Device>();
            OnDeviceUpdated?.Invoke(this, device);

            return Task.FromResult(SubscriberClient.Reply.Ack);
        }
    }
}
