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
    
    public partial class PIM_SSC_OUTPUT
    {
        public long output_id { get; set; }
        public Nullable<long> main_id_o { get; set; }
        public Nullable<int> hour_counter { get; set; }
        public Nullable<int> actoutput { get; set; }
        public Nullable<int> actoutput_pcs { get; set; }
    
        public virtual PIM_SSC_MAIN PIM_SSC_MAIN { get; set; }
    }
}
