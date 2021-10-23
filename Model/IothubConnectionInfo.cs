using System;

namespace BootstrapService
{
    public class IothubConnectionInfo
    {
        public bool Assigned { get; set; }
        public string PfxFile { get; set; }
        public string SecureString { get; set; }
        public string AssignedIoTHubName { get; set; }
        public DateTimeOffset CertificateExpiryDate { get; set; }
    }
}