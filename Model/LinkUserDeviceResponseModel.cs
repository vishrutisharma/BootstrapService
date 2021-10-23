using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BootstrapService.Model
{
    public class LinkUserDeviceResponseModel
    {
        public string DeviceId { get; set; }            
        public string DeviceType { get; set; }
        public string UserName { get; set; }
        public string UserLinked { get; set; }
    }
}
