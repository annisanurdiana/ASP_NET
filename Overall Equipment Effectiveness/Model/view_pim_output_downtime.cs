//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace OEE_SSC.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class view_pim_output_downtime
    {
        public long id { get; set; }
        public Nullable<System.TimeSpan> dt_start { get; set; }
        public Nullable<System.TimeSpan> dt_finish { get; set; }
        public string dt_code { get; set; }
        public string dt_notes { get; set; }
        public Nullable<long> dt_id { get; set; }
        public string dt_remarks { get; set; }
        public string dt_type { get; set; }
        public Nullable<int> shift { get; set; }
        public Nullable<System.DateTime> date { get; set; }
        public string op_kpk { get; set; }
        public string op_name { get; set; }
        public string no_machine { get; set; }
        public string partnumber { get; set; }
        public Nullable<int> shiftly_target { get; set; }
        public Nullable<int> actual_output { get; set; }
        public long id_user { get; set; }
        public Nullable<int> dt_duration_minutes { get; set; }
        public Nullable<int> not_running { get; set; }
        public Nullable<long> main_id { get; set; }
        public string we_ { get; set; }
        public Nullable<int> mc_area { get; set; }
    }
}
