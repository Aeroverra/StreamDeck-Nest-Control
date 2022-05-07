using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tech.Aerove.Tools.Nest.Models.WebCalls
{
    internal class RefreshTokenResponse
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        [JsonProperty("expires_in")]
        public long ExpiresIn { get; set; }

        [JsonProperty("scope")]
        public Uri Scope { get; set; }

        [JsonProperty("token_type")]
        public string TokenType { get; set; }
    }
}
