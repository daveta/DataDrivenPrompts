using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.BotBuilderSamples
{
    public class ModelDefinition
    {
        public ModelDefinition()
        {
        }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("matching_entities")]
        public List<string> MatchingEntities { get; set; } = new List<string>();

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }
    }
}
