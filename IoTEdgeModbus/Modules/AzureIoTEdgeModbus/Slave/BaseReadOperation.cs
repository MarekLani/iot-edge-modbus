﻿namespace AzureIoTEdgeModbus.Slave
{
    using System;
    using System.ComponentModel;
    using System.Configuration;

    /// <summary>
    /// The base data for a read operation used so that reported properties can be echoed back to the IoT Hub.
    /// </summary>
    public class BaseReadOperation
    {
        public int PollingInterval { get; set; }

        public byte UnitId { get; set; }

        [StringValidator(MinLength = 5)]
        public string StartAddress { get; set; }

        [IntegerValidator(MinValue = 1)]
        public UInt16 Count { get; set; }

        public string DisplayName { get; set; }

        [DefaultValue("DefaultCorrelationId")]
        public string CorrelationId { get; set; }

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