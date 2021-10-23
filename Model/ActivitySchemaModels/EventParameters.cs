using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BootstrapService.Model.ActivitySchemaModels
{
    public class EventParameters
    {
        public string activity_code_index { get; set; }
        public string activity { get; set; }
        public string description { get; set; }
        public string commercial_region { get; set; }
        public string service_region { get; set; }

    }
}
