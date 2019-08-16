using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.ComponentModel;
using System.Configuration;

namespace AzureIoTEdgeModbus.Slave
{

    public class WriteOperation : ModbusOperation
    {
        [JsonProperty(Required = Required.Always)]
        public string HwId { get; set; }

        [JsonProperty(Required = Required.Always)]
        public float Value { get; set; }

        [DefaultValue(false)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public bool IsFloat  { get; set; }
    }
}
