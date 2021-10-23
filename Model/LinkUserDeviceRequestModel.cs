using BootstrapService.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace BootstrapService.Model
{
    public class LinkUserDeviceRequestModel
    {
        public string DeviceId { get; set; }
        public string DeviceType { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
             
   
    }
}
