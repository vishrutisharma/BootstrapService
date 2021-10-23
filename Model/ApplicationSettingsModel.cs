using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BootstrapService.Model
{
    public class ApplicationSettingsModel
    {
        public string DEV_IOTHUB_CONNECTION_STRING { get; set; }
        public string QA_IOTHUB_CONNECTION_STRING { get; set; }
        public string ASPNETCORE_ENVIRONMENT { get; set; }
        public string QA_PROVISIONING_CONNECTION_STRING { get; set; }
        public string dev_IdScope { get; set; }
        public string qa_IdScope { get; set; }
        public string GLOBAL_DEVICE_ENDPOINT { get; set; }
        public string Password { get; set; }
        public string DEV_PROVISIONING_CONNECTION_STRING { get; set; }
    }
}
