using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BootstrapService.Model
{
    public class MultiDeviceProvisionResponseModel
    {
        public List<DeviceProvisionRepresentationModel> DeviceProvisioningStatus { get; set; }
    }

    public class DeviceProvisionRepresentationModel
    {
        public DeviceProvisionResponseModel ProvisionedDevices { get; set; }
        public string Status { get; set; }
    }
}
