using Microsoft.Azure.Devices.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BootstrapService.Model
{
    public class SendTestMessageRequestModel
    {
        public string DeviceId { get; set; }
        public string TopicType { get; set; }
        public string Frequency { get; set; }
        public string DeviceType { get; set; }
        public string SecureString { get; set; }
        public string AssignedIoTHubName { get; set; }
        public string PfxFile { get; set; }
        public string MessageType { get; set; }
        public string CertificateExpiryDate { get; set; }
        public string TestGuid { get; set; }
        public DeviceClient DeviceClient { get; set; }
    }
}
