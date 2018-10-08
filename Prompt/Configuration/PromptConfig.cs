using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.BotBuilderSamples
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum RunModeOptions
    {
        [EnumMember(Value = "training")]
        Training,
        [EnumMember(Value = "dev")]
        Dev,
        [EnumMember(Value = "none")]
        None,
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum PromptType
    {
        [EnumMember(Value = "int")]
        Int,
        [EnumMember(Value = "string")]
        String,
        [EnumMember(Value = "adaptive_card")]
        AdaptiveCard,
    }

    public class PromptConfig
    {
        public PromptConfig()
        {
        }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("prompt")]
        public string Prompt { get; set; }

        [JsonProperty("retry_prompt")]
        public string RetryPrompt { get; set; }

        [JsonProperty("type")]
        public PromptType Type { get; set; }

        [JsonProperty("run_mode")]
        public RunModeOptions RunMode { get; set; }

        [JsonProperty("model")]
        public ModelDefinition Model { get; set; }

        [JsonProperty("telemetry")]
        public List<TelemetryDefinition> Telemetry { get; set; }
    }
}
