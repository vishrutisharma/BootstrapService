namespace BootstrapService.Models
{
    using Microsoft.Azure.Cosmos.Table;

    public class ProvisionedDeviceIoTMetadata : TableEntity
    {
        public ProvisionedDeviceIoTMetadata()
        {

        }

        public ProvisionedDeviceIoTMetadata(string tenantDeviceTypeTopicKey, string deviceId)
        {
            PartitionKey = tenantDeviceTypeTopicKey;  //Epredia_RevosTissueProcessor_AssociatedIoTHub
            RowKey = deviceId;  // DeviceId
        }

        public string AssociatedIoTHub { get; set; }

        public string CertificateEndDate { get; set; }

        public string CertificateStartDate { get; set; }

        public string DeviceType { get; set; }
        public string DeviceId { get; set; }


    }
}