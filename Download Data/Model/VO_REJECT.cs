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
    
    public partial class VO_REJECT
    {
        public long reject_id { get; set; }
        public Nullable<long> main_id_rj { get; set; }
        public Nullable<int> mc_area { get; set; }
        public string no_machine { get; set; }
        public string partnumber { get; set; }
        public string mold { get; set; }
        public string we_ { get; set; }
        public Nullable<System.DateTime> date_ { get; set; }
        public Nullable<int> shift_ { get; set; }
        public Nullable<int> pn_stdoutput { get; set; }
        public Nullable<int> total_output_pcs { get; set; }
        public Nullable<long> hour_counter { get; set; }
        public Nullable<int> total_actoutput { get; set; }
        public Nullable<int> reject_qty { get; set; }
        public string reject_code { get; set; }
        public string rj_remarks { get; set; }
        public string reject_notes { get; set; }
        public Nullable<int> pn_ct { get; set; }
        public Nullable<int> pn_cav { get; set; }
        public Nullable<int> pn_set { get; set; }
        public Nullable<int> pengalioutput { get; set; }
        public string op_kpk { get; set; }
        public string op_name { get; set; }
    }
}
