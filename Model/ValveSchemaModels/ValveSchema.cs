using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BootstrapService.Model.ValveSchemaModels
{
    public class ValveSchema
    {
        public string id { get; set; }
        public string device_id { get; set; }
        public DateTime timestamp { get; set; }
        public string timezone { get; set; }
        public EventParameters eventParameters { get; set; }

    }

    public class EventParameters
    {
        public string colour_alcohol_count { get; set; }
        public string colour_alcohol_day { get; set; }
        public string colour_exchange_1 { get; set; }
        public string colour_exchange_2 { get; set; }
        public string colour_filter_charcoal { get; set; }
        public string colour_filter_downdraft { get; set; }
        public string colour_filter_fixative { get; set; }
        public string colour_fixative_1 { get; set; }
        public string colour_fixative_2 { get; set; }
        public string colour_flush_1 { get; set; }
        public string colour_flush_2 { get; set; }
        public string colour_flush_3 { get; set; }
        public string colour_wax_count { get; set; }
        public string colour_wax_day { get; set; }
        public string colour_wax_waste { get; set; }
        public string colour_xylene_count { get; set; }
        public string colour_xylene_day { get; set; }
        public string level_alcohol_1 { get; set; }
        public string level_alcohol_2 { get; set; }
        public string level_alcohol_3 { get; set; }
        public string level_alcohol_4 { get; set; }
        public string level_alcohol_5 { get; set; }
        public string level_alcohol_6 { get; set; }
        public string level_exchange_1 { get; set; }
        public string level_exchange_1_load { get; set; }
        public string level_exchange_1_load_show { get; set; }
        public string level_exchange_1_show { get; set; }
        public string level_exchange_2 { get; set; }
        public string level_exchange_2_load { get; set; }
        public string level_exchange_2_load_show { get; set; }
        public string level_exchange_2_show { get; set; }
        public string level_fixative_1 { get; set; }
        public string level_fixative_2 { get; set; }
        public string level_flush_1 { get; set; }
        public string level_flush_2 { get; set; }
        public string level_flush_3 { get; set; }
        public string level_wax_1 { get; set; }
        public string level_wax_2 { get; set; }
        public string level_wax_3 { get; set; }
        public string level_wax_waste { get; set; }
        public string level_wax_waste_show { get; set; }
        public string level_xylene_1 { get; set; }
        public string level_xylene_2 { get; set; }
        public string level_xylene_3 { get; set; }
        public string port_index { get; set; }
        public string usage_alcohol_count { get; set; }
        public string usage_alcohol_count_show { get; set; }
        public string usage_alcohol_day { get; set; }
        public string usage_alcohol_day_show { get; set; }
        public string usage_exchange_1 { get; set; }
        public string usage_exchange_1_show { get; set; }
        public string usage_exchange_2 { get; set; }
        public string usage_exchange_2_show { get; set; }
        public string usage_filter_charcoal { get; set; }
        public string usage_filter_charcoal_show { get; set; }
        public string usage_filter_downdraft { get; set; }
        public string usage_filter_downdraft_show { get; set; }
        public string usage_filter_fixative { get; set; }
        public string usage_filter_fixative_show { get; set; }
        public string usage_fixative_1 { get; set; }
        public string usage_fixative_1_show { get; set; }
        public string usage_fixative_2 { get; set; }
        public string usage_fixative_2_show { get; set; }
        public string usage_flush_1 { get; set; }
        public string usage_flush_1_show { get; set; }
        public string usage_flush_2 { get; set; }
        public string usage_flush_2_show { get; set; }
        public string usage_flush_3 { get; set; }
        public string usage_flush_3_show { get; set; }
        public string usage_wax_count { get; set; }
        public string usage_wax_count_show { get; set; }
        public string usage_wax_day { get; set; }
        public string usage_wax_day_show { get; set; }
        public string usage_wax_waste { get; set; }
        public string usage_wax_waste_show { get; set; }
        public string usage_xylene_count { get; set; }
        public string usage_xylene_count_show { get; set; }
        public string usage_xylene_day { get; set; }
        public string usage_xylene_day_show { get; set; }

    }
}
