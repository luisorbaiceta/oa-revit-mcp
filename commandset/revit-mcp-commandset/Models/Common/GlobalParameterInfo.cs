using Newtonsoft.Json;

namespace RevitMCPCommandSet.Models.Common
{
    public class GlobalParameterInfo
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("spec")]
        public string Spec { get; set; }

        [JsonProperty("value")]
        public object Value { get; set; }

        [JsonProperty("isReporting")]
        public bool IsReporting { get; set; }
    }
}
