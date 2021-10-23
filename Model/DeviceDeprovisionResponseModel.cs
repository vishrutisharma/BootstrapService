using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BootstrapService.Model
{
    public class DeviceDeprovisionResponseModel
    {
        public string DeviceId { get; set; }
        public string DeviceType { get; set; }
        public string DeProvision { get; set; }
    }
}
