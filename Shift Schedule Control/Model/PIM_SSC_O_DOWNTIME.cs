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
    
    public partial class PIM_SSC_O_DOWNTIME
    {
        public long downtime_id { get; set; }
        public Nullable<long> hour_counter { get; set; }
        public Nullable<long> main_id_dt { get; set; }
        public Nullable<System.TimeSpan> dt_start { get; set; }
        public Nullable<System.TimeSpan> dt_finish { get; set; }
        public Nullable<int> duration_minutes { get; set; }
        public string downtime_code { get; set; }
        public string downtime_notes { get; set; }
    
        public virtual pim_downtime pim_downtime { get; set; }
        public virtual PIM_SSC_MAIN PIM_SSC_MAIN { get; set; }
    }
}