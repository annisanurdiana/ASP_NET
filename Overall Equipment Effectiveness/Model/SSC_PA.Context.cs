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
    
    public partial class masbroEntities_PA : DbContext
    {
        public masbroEntities_PA()
            : base("name=masbroEntities_PA")
        {
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public virtual DbSet<pa_cd_defect_molded> pa_cd_defect_molded { get; set; }
        public virtual DbSet<pa_cd_downtime> pa_cd_downtime { get; set; }
        public virtual DbSet<pa_cd_reject> pa_cd_reject { get; set; }
        public virtual DbSet<pa_ssc_identity> pa_ssc_identity { get; set; }
        public virtual DbSet<pa_ssc_output> pa_ssc_output { get; set; }
        public virtual DbSet<pa_ssc_outputreject> pa_ssc_outputreject { get; set; }
    }
}
