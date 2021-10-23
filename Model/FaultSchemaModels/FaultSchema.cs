using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BootstrapService.Model.FaultSchemaModels
{
    public class FaultSchema
    {
        public string id { get; set; }
        public string device_id { get; set; }
        public DateTime timestamp { get; set; }
        public string timezone { get; set; }
        public EventParameters eventParameters { get; set; }
    }
    public class EventParameters
    {
        public string error_code_index { get; set; }
        public string error_code { get; set; }
        public string description { get; set; }
        public string commercial_region { get; set; }
        public string service_region { get; set; }
    }
}
