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
    
    public partial class View_PA_DEFECTMOLDED
    {
        public long defect_id { get; set; }
        public Nullable<long> main_id_dm { get; set; }
        public Nullable<System.DateTime> we_ { get; set; }
        public Nullable<System.DateTime> date_ { get; set; }
        public Nullable<int> shift_ { get; set; }
        public string no_index { get; set; }
        public string pn { get; set; }
        public Nullable<double> hourly_target { get; set; }
        public Nullable<int> shiftly_target { get; set; }
        public Nullable<int> total_actual_output { get; set; }
        public Nullable<int> total_output_pcs { get; set; }
        public Nullable<int> pn_stdoutput { get; set; }
        public Nullable<int> total_good_output { get; set; }
        public Nullable<int> totalDm { get; set; }
        public int hour_counter { get; set; }
        public Nullable<int> defect_qty { get; set; }
        public string defect_code { get; set; }
        public string DETAILS { get; set; }
        public string defect_notes { get; set; }
        public string op_kpk { get; set; }
        public string op_name { get; set; }
    }
}