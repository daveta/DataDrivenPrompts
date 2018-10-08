using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.BotBuilderSamples
{
    public class TelemetryDefinition
    {
        public TelemetryDefinition(string customEventName)
        {
            CustomEventName = customEventName;
        }

        [JsonProperty("custom_event_name")]
        public string CustomEventName { get; set; }

        [JsonProperty("fields")]
        public List<string> Fields { get; set; } = new List<string>();
    }
}
