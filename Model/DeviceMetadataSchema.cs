using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BootstrapService.Model
{
    public class DeviceMetadataSchema
    {
        public string id { get; set; }
        public string device_id { get; set; }
        public DateTime timestamp { get; set; }
        public string timezone { get; set; }
        public string software_version { get; set; }
        public string firmware_version { get; set; }
        public string service_region { get; set; }
        public string commercial_region { get; set; }
        public string device_timezone { get; set; }
    }
}
