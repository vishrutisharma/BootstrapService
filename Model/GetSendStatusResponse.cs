using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BootstrapService.Model
{
    public class GetSendStatusResponse
    {
        public string DeviceId { get; set; }
        public string UserLinkStatus { get; set; }
        public string DeviceSendStatus { get; set; }
    }
}
