using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BootstrapService.Model
{
    public class MainTelemetrySchema
    {
        public string id { get; set; }
        public string device_id { get; set; }
        public DateTime timestamp { get; set; }
        public string timezone { get; set; }
        public int pressure_chamber { get; set; }
        public int specific_gravity { get; set; }
        public int specific_gravity_colour { get; set; }
        public int protocol_step_index { get; set; }
        public int protocol_time_remaining { get; set; }
        public int temperature_chamber_base_0 { get; set; }
        public int temperature_chamber_base_1 { get; set; }
        public int temperature_chamber_base_2 { get; set; }
        public int temperature_chamber_base_3 { get; set; }
        public int temperature_chamber_base_4 { get; set; }
        public int temperature_chamber_base_5 { get; set; }
        public int temperature_chamber_fluid_2 { get; set; }
        public int temperature_chamber_fluid_3 { get; set; }
        public int temperature_chamber_level_2 { get; set; }
        public int temperature_chamber_level_3 { get; set; }
        public int temperature_valve { get; set; }
        public int temperature_waxbath_base_1 { get; set; }
        public int temperature_waxbath_base_2 { get; set; }
        public int temperature_waxbath_base_3 { get; set; }
        public int temperature_waxbath_fluid_1 { get; set; }
        public int temperature_waxbath_fluid_2 { get; set; }
        public int temperature_waxbath_fluid_3 { get; set; }
        public int temperature_waxbath_level_1 { get; set; }
        public int temperature_waxbath_level_2 { get; set; }
        public int temperature_waxbath_level_3 { get; set; }
        public int temperature_waxpipe_1 { get; set; }
        public int temperature_waxpipe_2 { get; set; }
        public int temperature_waxpipe_3 { get; set; }
        public int temperature_reagent_alcohol { get; set; }
        public int temperature_reagent_xylene { get; set; }
        public int chamber_fluid_level { get; set; }
        public int chamber_level_sensor_1 { get; set; }
        public int chamber_level_sensor_2 { get; set; }
        public int chamber_level_sensor_3 { get; set; }
        public int valve_position { get; set; }
    }
}
