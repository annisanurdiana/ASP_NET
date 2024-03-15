using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OEE_SSC.Models
{
    public class pimListData
    {
        // upload file
        public IEnumerable<ObjFile> ObjFiles { get; set; }

        // Week Ending List
        public List<DateTime> PIM_SSC_MAINs_WE { get; set; }

        // new PIM table
        public IEnumerable<PIM_SSC_MAIN> PIM_SSC_MAINs { get; set; }
        public IEnumerable<PIM_SSC_OUTPUT> PIM_SSC_OUTPUTs { get; set; }
        public IEnumerable<PIM_SSC_CALCULATION> PIM_SSC_CALCULATIONs { get; set; }
        public IEnumerable<PIM_SSC_O_REJECT> PIM_SSC_O_REJECTs { get; set; }
        public IEnumerable<PIM_SSC_O_DOWNTIME> PIM_SSC_O_DOWNTIMEs { get; set; }

        // view for OEE
        public IEnumerable<VO_REJECT> VO_REJECT_TOPs { get; set; }
        public IEnumerable<VO_REJECT> VO_REJECTs { get; set; }
        public IEnumerable<VO_REJECT_PP> VO_REJECT_PPs { get; set; }
        public IEnumerable<VO_DOWNTIME> VO_DOWNTIME_TOPs { get; set; }
        public IEnumerable<VO_DOWNTIME> VO_DOWNTIMEs { get; set; }
        public IEnumerable<VO_MAIN_CALC> VO_MAIN_CALCs { get; set; }
        public IEnumerable<VO_MAIN_CALC> VO_MAIN_CALC_AREAs { get; set; }
        public IEnumerable<VO_MAIN_CALC> VO_MAIN_CALC_TOPs { get; set; }

        // TOP 5 DT & RJ
        public IEnumerable<VO_DOWNTIME> TOP5_VO_DOWNTIMEs_MC { get; set; }
        public IEnumerable<VO_DOWNTIME> TOP5_VO_DOWNTIMEs_PN { get; set; }
        public IEnumerable<VO_DOWNTIME> TOP5_VO_DOWNTIMEs_MN { get; set; }
        public IEnumerable<VO_REJECT> TOP5_VO_REJECTs_MC { get; set; }
        public IEnumerable<VO_REJECT> TOP5_VO_REJECTs_PN { get; set; }
        public IEnumerable<VO_REJECT> TOP5_VO_REJECTs_MN { get; set; }

        public List<PIM_SSC_O_REJECT> reject_table { get; set; }

        // schedule table
        public IEnumerable<East_Schedule> East_Schedules { get; set; }

        public IEnumerable<pim_downtime> pim_Downtimes { get; set; }
        public IEnumerable<pim_reject> pim_Rejects { get; set; }
        public IEnumerable<view_pim_output_downtime> view_Pim_Output_Downtime { get; set; }
        public IEnumerable<view_pim_output_reject> view_Pim_Output_Reject { get; set; }

        // Database punya tetangga - Engineering
        public IEnumerable<MDS_PART_NUMBER_LIST> mds_PART_NUMBER_LISTs { get; set; }


        // schedule table
        public IEnumerable<pim_kpi_performance> pim_KPI_Performances { get; set; }

        // auto loading database operator
        public IEnumerable<pim_autoloading> pim_Autoloadings { get; set; }
        public IEnumerable<string> pim_op_skill { get; set; }
        public IEnumerable<string> pim_Autoloadings_leader { get; set; }
        // auto loading database skill list
        public IEnumerable<pim_autoloading_skill> pim_Autoloading_Skills { get; set; }
        public IEnumerable<string> pim_Autoloading_Skillcodes { get; set; }


    }
}