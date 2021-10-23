using System;

namespace BootstrapService
{
    public class DeviceInfo
    {
        public string DeviceType { get; set; }
        public string IoTHubName { get; set; }
        public DateTimeOffset CertificateExpiryDate { get; set; }

    }
}