using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BootstrapService.Model
{
    public class UpdateMetadataSchema
    {
        public string id { get; set; }
        public string device_id { get; set; }
        public string request { get; set; }
        public string chamber_status { get; set; }
        public string protocol_name { get; set; }
        public string reagent_name { get; set; }
        public DateTime timestamp { get; set; }
        public string timezone { get; set; }
        public DateTime endTime { get; set; }

    }
}
