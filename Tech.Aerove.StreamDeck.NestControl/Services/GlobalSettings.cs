using Aeroverra.StreamDeck.Client.Services;

namespace Aeroverra.StreamDeck.NestControl.Services
{
    internal class GlobalSettings : IGlobalSettings
    {
        public bool IsReady { get; set; } = false;

        public string? ClientId { get; set; }
        public string? ClientSecret { get; set; }
        public string? ProjectId { get; set; }
        public string? Code { get; set; }
        public string? Scope { get; set; }
        public string? SubscriptionId { get; set; }
        public string? CloudProjectId { get; set; }
        public string? RefreshToken { get; set; }
        public bool? Setup { get; set; }
        public string? PiDevices { get; set; }

    }
}
