﻿//------------------------------------------------------------------------------
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
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    
    public partial class masbroEntitiessSSC_PA : DbContext
    {
        public masbroEntitiessSSC_PA()
            : base("name=masbroEntitiessSSC_PA")
        {
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public virtual DbSet<pa_cd_defectmolded> pa_cd_defectmolded { get; set; }
        public virtual DbSet<pa_cd_downtime> pa_cd_downtime { get; set; }
        public virtual DbSet<pa_cd_reject> pa_cd_reject { get; set; }
        public virtual DbSet<pa_part_number_list> pa_part_number_list { get; set; }
        public virtual DbSet<pa_ssc_main> pa_ssc_main { get; set; }
        public virtual DbSet<pa_ssc_output> pa_ssc_output { get; set; }
        public virtual DbSet<pa_ssc_output_defectmolded> pa_ssc_output_defectmolded { get; set; }
        public virtual DbSet<pa_ssc_outputcalculated> pa_ssc_outputcalculated { get; set; }
        public virtual DbSet<pa_ssc_outputdowntime> pa_ssc_outputdowntime { get; set; }
        public virtual DbSet<pa_ssc_outputreject> pa_ssc_outputreject { get; set; }
        public virtual DbSet<pa_user_kpk> pa_user_kpk { get; set; }
        public virtual DbSet<View_PA_DEFECTMOLDED> View_PA_DEFECTMOLDED { get; set; }
        public virtual DbSet<View_PA_DOWNTIME_> View_PA_DOWNTIME_ { get; set; }
        public virtual DbSet<View_PA_MAIN_CALC> View_PA_MAIN_CALC { get; set; }
        public virtual DbSet<View_PA_REJECT> View_PA_REJECT { get; set; }
    }
}
