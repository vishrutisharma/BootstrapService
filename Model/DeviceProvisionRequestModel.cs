using BootstrapService.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace BootstrapService.Model
{
    public class DeviceProvisionRequestModel
    {
        public string DeviceId { get; set; }
        public string DeviceType { get; set; }
        public string DeviceSoftwareVersion { get; set; }
        public string DeviceFirmwareVersion { get; set; }
        public string ServiceRegion { get; set; }
        public string CommercialRegion { get; set; }
        public string HubRegion { get; set; }
        public string DeviceTimeZone { get; set; }

    }
}
