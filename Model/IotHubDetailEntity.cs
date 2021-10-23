using Microsoft.Azure.Cosmos.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BootstrapService.Model
{
    public class IotHubDetailEntity : TableEntity
    {
        public string RegionId { get; set; }
        public string  RegionName { get; set; }
        public string IotHubDetail { get; set; }
    }
}
