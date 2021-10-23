using Microsoft.Azure.Cosmos.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BootstrapService.Model
{
    public class ProvisionedDeviceMetadata : TableEntity
    {
        public ProvisionedDeviceMetadata()
        {
        }

        public ProvisionedDeviceMetadata(string tenantDeviceTypeTopicKey, string deviceId)
        {
            PartitionKey = tenantDeviceTypeTopicKey;  //Epredia_RevosTissueProcessor_DeviceMetadata
            RowKey = deviceId;  // DeviceId
        }
        public string deviceName { get; set; }
        public string deviceTimeZone { get; set; }
        public string softwareVersion { get; set; }
        public string firmwareVersion { get; set; }
        public string deviceType { get; set; }
        public string commercialRegion { get; set; }
        public string serviceRegion { get; set; }
        public string hubRegion { get; set; }
        public string deviceStatus { get; set; }
        public string deviceSerialNumber { get; set; }
        public string deviceStatusTimestamp { get; set; }
    }
}