using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BootstrapService.Model.ActivitySchemaModels
{
    public class ActivitySchema
    {
        public string id { get; set; }
        public string device_id { get; set; }
        public DateTime timestamp { get; set; }
        public string timezone { get; set; }
        public EventParameters eventParameters { get; set; }
    }
}
