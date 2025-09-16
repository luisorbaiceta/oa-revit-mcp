using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitMCPCommandSet.Models.Common
{
    public class ParameterUpdateInfo
    {
        [JsonProperty("elementId")]
        public int ElementId { get; set; }

        [JsonProperty("parameterName")]
        public string ParameterName { get; set; }

        [JsonProperty("parameterValue")]
        public object ParameterValue { get; set; }
    }
}
