namespace AzureIoTEdgeModbus.Slave
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;
    using System;
    using System.ComponentModel;
    using System.Configuration;

    /// <summary>
    /// The base data for a read operation used so that reported properties can be echoed back to the IoT Hub.
    /// </summary>
    public class ReadOperation : ModbusOperation
    {
        [DefaultValue(1000)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public int PollingInterval { get; set; }

        [IntegerValidator(MinValue = 1)]
        public UInt16 Count { get; set; }

        [JsonProperty(Required=Required.Always)]
        public string DisplayName { get; set; }

        [DefaultValue("DefaultCorrelationId")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public string CorrelationId { get; set; }

        [JsonIgnore]
        public byte[] Request;

        [JsonIgnore]
        public byte[] Response;

        [JsonIgnore]
        public int RequestLen;

        [JsonIgnore]
        public string OutFormat
            => (this.StartAddress.Length == 5 ? "{0}{1:0000}" : this.StartAddress.Length == 6 ? "{0}{1:00000}" : string.Empty);


        public void Verify()
        {
            Console.WriteLine($"Operation Configuration Used: {Environment.NewLine}");
            Console.WriteLine($"PollingInterval: {this.PollingInterval}");
            Console.WriteLine($"UnitId: {this.UnitId}");
            Console.WriteLine($"StartAddress: {this.StartAddress}");
            Console.WriteLine($"Count: {this.Count}");
            Console.WriteLine($"DisplayName: {this.DisplayName}");
            Console.WriteLine($"CorrelationId: {this.CorrelationId}");
        }
    }
}