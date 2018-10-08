using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System.Runtime.Serialization;

namespace Microsoft.BotBuilderSamples
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ResultType
    {
        [EnumMember(Value = "string")]
        String,
        [EnumMember(Value = "int")]
        Int,
        [EnumMember(Value = "none")]
        None,
    }

    public class DataDrivenResult
    {
        public DataDrivenResult()
        {
        }

        public string Intent { get; set; } = "None";

        public JObject Updates { get; set; }

        public object Result { get; set; }

        public ResultType Type { get; set; }

        public string Message { get; set; }
    }
}
