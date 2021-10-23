using Microsoft.Azure.Cosmos.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BootstrapService.Model
{
    public class DeviceTypeInformationEntity : TableEntity
    {
        public string LocalizedName { get; set; }
        public string HostedAppURL { get; set; }
        public string HelpDoc { get; set; }
        public string Logo { get; set; }
    }
}
