using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BootstrapService.Model
{
    public class ProvisionPerformanceTestDeviceRequestModel
    {
        public string TestGuid { get; set; }
        public string Environment { get; set; }
        public string EncryptedToken { get; set; }
        public string EncryptedRandomKey { get; set; }
        public int Delay { get; set; }
    }
}
