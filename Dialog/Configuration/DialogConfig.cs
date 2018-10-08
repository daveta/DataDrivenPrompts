using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.BotBuilderSamples
{
    public class DialogConfig
    {
        public DialogConfig()
        {
        }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("prompts")]
        public List<string> Prompts { get; set; }

        [JsonProperty("dispatch_intents")]
        public List<string> DispatchIntents { get; set; }

        [JsonProperty("run_mode")]
        public RunModeOptions RunMode { get; set; }

        [JsonProperty("telemetry")]
        public List<TelemetryDefinition> Telemetry { get; set; }
    }
}
