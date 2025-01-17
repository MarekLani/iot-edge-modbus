﻿namespace AzureIoTEdgeModbus.DeviceTwin
{
    using AzureIoTEdgeModbus.Configuration;
    using AzureIoTEdgeModbus.Wrappers;
    using Newtonsoft.Json;

    using System;
    using System.Configuration;
    using System.Threading;
    using System.Threading.Tasks;

    public class DeviceTwinConfiguration<T> : DeviceConfiguration<T>
    {
        private IDeviceConfiguration<T> DeviceConfiguration { get; }
        private IModuleClient IotHubModuleClient { get; }

        public DeviceTwinConfiguration(IDeviceConfiguration<T> deviceConfiguration, IModuleClient moduleClient)
        {
            this.DeviceConfiguration = deviceConfiguration;
            this.IotHubModuleClient = moduleClient;
        }

        protected override async Task<T> GetConfigurationAsync(CancellationToken cancellationToken)
        {
            try
            {
                // Get desired properties from twin.
                var twin = await this.IotHubModuleClient.GetTwinAsync(cancellationToken).ConfigureAwait(false);
                Console.WriteLine($"Desired properties retrieved from twin: {Environment.NewLine}{twin.Properties.Desired}");

                var desiredProperties = JsonConvert.SerializeObject(twin.Properties.Desired);

                return this.DeserialiseDesiredProperties(desiredProperties);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not use desired properties from twin, error: {ex.Message}");

                return this.DeviceConfiguration == null ? throw new ConfigurationErrorsException("Could not find secondary configuration store.")
                    : await this.DeviceConfiguration.GetDeviceConfigurationAsync(cancellationToken).ConfigureAwait(false);
            }
        }
    }
}