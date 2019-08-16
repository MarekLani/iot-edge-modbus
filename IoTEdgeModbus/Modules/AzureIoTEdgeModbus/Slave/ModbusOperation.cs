using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Text;

namespace AzureIoTEdgeModbus.Slave
{
    public class ModbusOperation
    {
        public byte UnitId { get; set; }

        [StringValidator(MinLength = 5)]
        [JsonProperty(Required = Required.Always)]
        public string StartAddress { get; set; }

        [DefaultValue(true)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public bool IsSimpleValue { get; set; }

        public string ValueType { get; set; }

        [JsonIgnore]
        public byte Entity
         => (Encoding.ASCII.GetBytes(this.StartAddress, 0, 1)[0]);

        [JsonIgnore]
        public ushort Address
            => ((ushort)(Convert.ToInt32(this.StartAddress.Substring(1)) - 1));

        /// <summary>
        /// Only Read Supported
        /// </summary>
        [JsonIgnore]
        public byte FunctionCode
           => (
                (char)this.Entity == (char)EntityType.CoilStatus ? (byte)FunctionCodeType.ReadCoils :
                (char)this.Entity == (char)EntityType.HoldingRegister ? (byte)FunctionCodeType.ReadHoldingRegisters :
                (char)this.Entity == (char)EntityType.InputStatus ? (byte)FunctionCodeType.ReadInputs :
                (char)this.Entity == (char)EntityType.InputRegister ? (byte)FunctionCodeType.ReadInputRegisters :
                byte.MinValue
            );
    }
}
