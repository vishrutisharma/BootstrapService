using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BootstrapService.Model
{
    public class PowerTelemetrySchema
    {
        public string id { get; set; }
        public string device_id { get; set; }
        public DateTime timestamp { get; set; }
        public string timezone { get; set; }
        public int power_rail_1V1 { get; set; }
        public int power_rail_2V5 { get; set; }
        public int power_rail_3V3 { get; set; }
        public int power_rail_3V3_filtered { get; set; }
        public int power_rail_5V { get; set; }
        public int power_rail_12V { get; set; }
        public int power_rail_12V_filtered { get; set; }
        public int power_rail_24V { get; set; }
        public int power_rail_48V_electronics_wax { get; set; }
        public int power_rail_48V_chamber_valve { get; set; }
        public int battery_1_voltage { get; set; }
        public int battery_2_voltage { get; set; }
        public int battery_3_voltage { get; set; }
        public int battery_4_voltage { get; set; }
        public int battery_1_current { get; set; }
        public int battery_2_current { get; set; }
        public int battery_3_current { get; set; }
        public int battery_4_current { get; set; }
    }
}
