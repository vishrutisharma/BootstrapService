using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BootstrapService.Model
{
    public class DeprovisionDeviceByTestGuidRequestModel
    {
        public string DeviceId { get; set; }
        public string SecureString { get; set; }
        public string AssignedIoTHubName { get; set; }
        public string CertificateExpiryDate { get; set; }
        public string PfxFile { get; set; }
        public string UserLinkStatus { get; set; }
        public string DeviceSendStatus { get; set; }
        public string TestGuid { get; set; }
        public string Environment { get; set; }
    }
}
