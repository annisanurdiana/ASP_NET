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
    
    public partial class VO_SCRAP
    {
        public long id { get; set; }
        public Nullable<int> shift_ { get; set; }
        public Nullable<System.DateTime> date_ { get; set; }
        public string partnumber { get; set; }
        public Nullable<int> quantity { get; set; }
        public Nullable<int> area_code { get; set; }
        public string area_name { get; set; }
        public Nullable<int> scrap_number { get; set; }
        public string scrap_code { get; set; }
        public string check_ { get; set; }
        public string S_NAME { get; set; }
        public string SCRAP_REMARKS { get; set; }
        public string Expr1 { get; set; }
    }
}
