using BootstrapService.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace BootstrapService.Model
{
    public class DeviceDeprovisionRequestModel
    {
        public string DeviceId { get; set; }
        public string DeviceType { get; set; }
    }
}
