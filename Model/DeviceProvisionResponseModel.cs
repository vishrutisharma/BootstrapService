using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BootstrapService.Model
{
    public class DeviceProvisionResponseModel
    {
        public string DeviceId { get; set; }
        public string IotHubEndPoint { get; set; }
        public string PfxKey { get; set; }
        public string SecureString { get; set; }
        public DateTimeOffset? CertificateExpiryDate { get; set; }
    }
}
