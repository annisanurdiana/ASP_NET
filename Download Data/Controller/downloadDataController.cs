using OEE_SSC.Models;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.IO;
using System.Linq;
using System.Web;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Web.Mvc;
using System.Windows;
using System.Windows.Media.Media3D;
using System.Xml.Linq;
using static OEE_SSC.Models.pim_scrap;
using static System.Data.Entity.Infrastructure.Design.Executor;
using Microsoft.Ajax.Utilities;

namespace OEE_SSC.Controllers
{
    public class downloadDataController : Controller
    {
        // START - ENTITIES DATA MODEL //
        masbroEntitiesPIM ssc_db = new masbroEntitiesPIM(); // SSC db
        //MMSEntities ssc_db = new MMSEntities(); // PIM database here 
        MDSEntities pn_db = new MDSEntities(); // partnumber_official db
        //MMSEntitiesScrap2 scrap_db = new MMSEntitiesScrap2(); // pim_scrap db

        // GET: downloadData
        public ActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public ActionResult downloadData(DateTime? dateStart = null, DateTime? dateFinish = null)
        {
            // SESSION
            int inputShift = (int)(System.Web.HttpContext.Current.Session["myShift"] ?? 0);
            DateTime? dateFilter = (DateTime?)System.Web.HttpContext.Current.Session["myDate"];
            DateTime filterDate = dateFilter ?? DateTime.Now.Date;

            DateTime now = DateTime.Now;
            DateTime startDate = dateStart ?? now.Date;
            DateTime finishDate = dateFinish ?? now.Date;

            if (inputShift == 0 || dateFilter == null)
            {
                // ------------------------------------------------------------------------------------ //
                // Jika inputShift bernilai 0, maka set nilai inputShift menjadi 2

                if (now.TimeOfDay >= new TimeSpan(7, 10, 0) && now.TimeOfDay < new TimeSpan(15, 40, 0))
                {
                    inputShift = 2;
                }
                else if (now.TimeOfDay >= new TimeSpan(15, 40, 0) && now.TimeOfDay < new TimeSpan(22, 40, 0))
                {
                    inputShift = 3;
                }
                else
                {
                    inputShift = 1;
                    filterDate = now.AddDays(1).Date;
                }
            }

            // Assign value to ViewBag
            ViewBag.inputShift = inputShift;
            ViewBag.FilterDate = filterDate;

            ViewBag.startDate = startDate;
            ViewBag.finishDate = finishDate;

            // ---------------------------------------------- //
            int inputShiftNow = 0;
            if (now.TimeOfDay >= new TimeSpan(7, 10, 0) && now.TimeOfDay < new TimeSpan(15, 40, 0))
            {
                inputShiftNow = 2;
            }
            else if (now.TimeOfDay >= new TimeSpan(15, 40, 0) && now.TimeOfDay < new TimeSpan(22, 40, 0))
            {
                inputShiftNow = 3;
            }
            else
            {
                inputShiftNow = 1;
                filterDate = now.AddDays(1).Date;
            }

            DateTime now2 = DateTime.Now;
            ViewBag.DateNow = now2;
            ViewBag.inputShiftNow = inputShiftNow;

            // ---------------------------------------------- //

            var table_data = new pimListData
            {
                // database user output select data yang teriisi dan yang kpk nya bukan kpk Diana Testing hehe...
                VO_MAIN_CALCs = ssc_db.VO_MAIN_CALC.Where(x => x.date_ >= startDate && x.date_ <= finishDate && x.mold != null && x.op_kpk != null && x.op_kpk != "00220").
                    OrderBy(x => x.date_).ThenBy(x => x.shift_).ThenBy(x => x.no_machine).ToList(),

                // read data - relationship table downtime with user, select kecuali kode : PD09 (not running bukan downtime)
                VO_DOWNTIMEs = ssc_db.VO_DOWNTIME.Where(x => x.date_ >= startDate && x.date_ <= finishDate && x.mold != null && x.op_kpk != null && x.op_kpk != "00220" && x.downtime_code != "PD09").
                    OrderBy(x => x.date_).ThenBy(x => x.shift_).ThenBy(x => x.no_machine).ThenBy(x => x.dt_start).ToList(),

                // >>>>> OUTPUT REJECT dijumlah berdasarkan PPM jika MC, PN, dan Remarks sama
                VO_REJECTs = ssc_db.VO_REJECT.Where(x => x.date_ >= startDate && x.date_ <= finishDate && x.mold != null && x.op_kpk != null && x.op_kpk != "00220" && x.total_actoutput != null && x.total_actoutput > 0).
                GroupBy(x => new { x.we_, x.date_, x.shift_, x.no_machine, x.partnumber, x.reject_code, x.rj_remarks })
                .Select(g => new
                {
                    we_ = g.Key.we_,
                    date_ = g.Key.date_,
                    shift_ = g.Key.shift_,
                    no_machine = g.Key.no_machine,
                    partnumber = g.Key.partnumber,
                    reject_code = g.Key.reject_code,
                    rj_remarks = g.Key.rj_remarks,
                    reject_qty = g.Sum(x => x.reject_qty),
                    total_actoutput = g.FirstOrDefault().total_actoutput
                    //rj_rate2 = Math.Round(((decimal)g.Sum(x => x.reject_qty) / (decimal)g.FirstOrDefault().total_actoutput) * 100, 2)

                })
                .OrderBy(x => x.date_).ThenBy(x => x.shift_).ThenBy(x => x.no_machine).ThenBy(x => x.reject_code).ToList()
                .Select(x => new VO_REJECT
                {
                    we_ = x.we_,
                    date_ = x.date_,
                    shift_ = x.shift_,
                    no_machine = x.no_machine,
                    partnumber = x.partnumber,
                    reject_code = x.reject_code,
                    rj_remarks = x.rj_remarks,
                    reject_qty = x.reject_qty,
                }).ToList(),

                // Menampilkan Data OEE
                VO_MAIN_CALC_TOPs = ssc_db.VO_MAIN_CALC.Where(x => x.date_ >= startDate && x.date_ <= finishDate && x.total_time != null && x.mold != null && x.op_kpk != null && x.op_kpk != "00220" && x.total_time > 0)
                .GroupBy(x => new { x.we_, x.date_, x.shift_, x.no_machine })
                .Select(g => new
                {
                    we_ = g.Key.we_,
                    date_ = g.Key.date_,
                    shift_ = g.Key.shift_,
                    no_machine = g.Key.no_machine,
                    total_time = g.Sum(x => x.total_time),
                    total_good_output = g.Sum(x => x.total_good_output),
                    total_actoutput = g.Sum(x => x.total_actoutput),
                    total_output_pcs = g.Sum(x => x.total_output_pcs),
                    pn_stdoutput = g.Sum(x => x.pn_stdoutput),
                    pn_operatingtime = g.Sum(x => x.pn_operatingtime),
                    totalRJ = g.Sum(x => x.totalRJ),
                    totalDT = g.Sum(x => x.totalDT),
                    totalNotRun = g.Sum(x => x.totalNotRun),
                    performance = (g.Sum(x => x.pn_stdoutput) == 0) ? 0 : Math.Round(((decimal)(g.Sum(x => x.total_output_pcs) ?? 0) / (decimal)g.Sum(x => x.pn_stdoutput)) * 100, 2)
                }).
                OrderBy(x => x.date_).ThenBy(x => x.shift_).ThenBy(x => x.no_machine).ToList()
                .Select(x => new VO_MAIN_CALC
                {
                    we_ = x.we_,
                    date_ = x.date_,
                    shift_ = x.shift_,
                    no_machine = x.no_machine,
                    total_time = x.total_time,
                    total_good_output = x.total_good_output,
                    total_actoutput = x.total_actoutput,
                    total_output_pcs = x.total_output_pcs,
                    pn_stdoutput = x.pn_stdoutput,
                    pn_operatingtime = x.pn_operatingtime,
                    totalRJ = x.totalRJ,
                    totalDT = x.totalDT,
                    totalNotRun = x.totalNotRun
                }).ToList()
            };



            // Mengirimkan data_scrap ke tampilan
            return View(table_data);
        }



        [HttpGet]
        public ActionResult displayData(DateTime? dateStart = null, DateTime? dateFinish = null)
        {
            DateTime? dateFilter = (DateTime?)System.Web.HttpContext.Current.Session["myDate"];
            DateTime filterDate = dateFilter ?? DateTime.Now.Date;
            ViewBag.FilterDate = filterDate;

            DateTime now = DateTime.Now;
            DateTime startDate = dateStart ?? now.Date;
            DateTime finishDate = dateFinish ?? now.Date;
            // ---------------------------------------------- //
            int inputShiftNow = 0;
            if (now.TimeOfDay >= new TimeSpan(7, 10, 0) && now.TimeOfDay < new TimeSpan(15, 40, 0))
            {
                inputShiftNow = 2;
            }
            else if (now.TimeOfDay >= new TimeSpan(15, 40, 0) && now.TimeOfDay < new TimeSpan(22, 40, 0))
            {
                inputShiftNow = 3;
            }
            else
            {
                inputShiftNow = 1;
                dateFinish = now.AddDays(1).Date;
            }

            ViewBag.DateNow = now;
            ViewBag.inputShiftNow = inputShiftNow;

            ViewBag.startDate = startDate;
            ViewBag.finishDate = finishDate;

            // ---------------------------------------------- //

            var table_data = new pimListData
            {
                // database user output select data yang teriisi dan yang kpk nya bukan kpk Diana Testing hehe...
                VO_MAIN_CALCs = ssc_db.VO_MAIN_CALC.Where(x => x.date_ >= startDate && x.date_ <= finishDate && x.mold != null && x.op_kpk != null && x.op_kpk != "00220").
                    OrderBy(x => x.date_).ThenBy(x => x.shift_).ThenBy(x => x.no_machine).ToList(),

                // read data - relationship table downtime with user, select kecuali kode : PD09 (not running bukan downtime)
                VO_DOWNTIMEs = ssc_db.VO_DOWNTIME.Where(x => x.date_ >= startDate && x.date_ <= finishDate && x.mold != null && x.op_kpk != null && x.op_kpk != "00220").
                    OrderBy(x => x.date_).ThenBy(x => x.shift_).ThenBy(x => x.no_machine).ThenBy(x => x.dt_start).ToList(),

                // >>>>> OUTPUT REJECT dijumlah berdasarkan PPM jika MC, PN, dan Remarks sama
                VO_REJECTs = ssc_db.VO_REJECT.Where(x => x.date_ >= startDate && x.date_ <= finishDate && x.mold != null && x.op_kpk != null && x.op_kpk != "00220" && x.total_actoutput != null && x.total_actoutput > 0).
                GroupBy(x => new { x.we_, x.date_, x.shift_, x.no_machine, x.partnumber, x.reject_code, x.rj_remarks })
                .Select(g => new
                {
                    we_ = g.Key.we_,
                    date_ = g.Key.date_,
                    shift_ = g.Key.shift_,
                    no_machine = g.Key.no_machine,
                    partnumber = g.Key.partnumber,
                    reject_code = g.Key.reject_code,
                    rj_remarks = g.Key.rj_remarks,
                    reject_qty = g.Sum(x => x.reject_qty),
                    total_actoutput = g.FirstOrDefault().total_actoutput
                    //rj_rate2 = Math.Round(((decimal)g.Sum(x => x.reject_qty) / (decimal)g.FirstOrDefault().total_actoutput) * 100, 2)

                })
                .OrderBy(x => x.date_).ThenBy(x => x.shift_).ThenBy(x => x.no_machine).ThenBy(x => x.reject_code).ToList()
                .Select(x => new VO_REJECT
                {
                    we_ = x.we_,
                    date_ = x.date_,
                    shift_ = x.shift_,
                    no_machine = x.no_machine,
                    partnumber = x.partnumber,
                    reject_code = x.reject_code,
                    rj_remarks = x.rj_remarks,
                    reject_qty = x.reject_qty,
                }).ToList(),

                // Menampilkan Data OEE
                VO_MAIN_CALC_TOPs = ssc_db.VO_MAIN_CALC.Where(x => x.date_ >= startDate && x.date_ <= finishDate && x.total_time != null && x.mold != null && x.op_kpk != null && x.op_kpk != "00220" && x.total_time > 0)
                .GroupBy(x => new { x.we_, x.date_, x.shift_, x.no_machine })
                .Select(g => new
                {
                    we_ = g.Key.we_,
                    date_ = g.Key.date_,
                    shift_ = g.Key.shift_,
                    no_machine = g.Key.no_machine,
                    total_time = g.Sum(x => x.total_time),
                    total_good_output = g.Sum(x => x.total_good_output),
                    total_actoutput = g.Sum(x => x.total_actoutput),
                    total_output_pcs = g.Sum(x => x.total_output_pcs),
                    pn_stdoutput = g.Sum(x => x.pn_stdoutput),
                    pn_operatingtime = g.Sum(x => x.pn_operatingtime),
                    totalRJ = g.Sum(x => x.totalRJ),
                    totalDT = g.Sum(x => x.totalDT),
                    totalNotRun = g.Sum(x => x.totalNotRun),
                    performance = (g.Sum(x => x.pn_stdoutput) == null || g.Sum(x => x.pn_stdoutput) == 0) ? 0 : Math.Round(((decimal)(g.Sum(x => x.total_output_pcs) ?? 0) / (decimal)g.Sum(x => x.pn_stdoutput.Value)) * 100, 2)

                }).
                OrderBy(x => x.date_).ThenBy(x => x.shift_).ThenBy(x => x.no_machine).ToList()
                .Select(x => new VO_MAIN_CALC
                {
                    we_ = x.we_,
                    date_ = x.date_,
                    shift_ = x.shift_,
                    no_machine = x.no_machine,
                    total_time = x.total_time,
                    total_good_output = x.total_good_output,
                    total_actoutput = x.total_actoutput,
                    total_output_pcs = x.total_output_pcs,
                    pn_stdoutput = x.pn_stdoutput,
                    pn_operatingtime = x.pn_operatingtime,
                    totalRJ = x.totalRJ,
                    totalDT = x.totalDT,
                    totalNotRun = x.totalNotRun
                }).ToList(),


                // CALCULATE COMPLETENESS PER AREA
                VO_MAIN_CALC_AREAs = ssc_db.VO_MAIN_CALC
                .Where(x => x.date_ >= startDate && x.date_ <= finishDate && x.total_time != null &&
                            x.mold != null && x.op_kpk != null && x.op_kpk != "00220" && x.total_time > 0)
                .GroupBy(x => new { x.we_, x.date_, x.shift_, x.no_machine, x.mc_area })
                .Select(g => new
                {
                    we_ = g.Key.we_,
                    date_ = g.Key.date_,
                    shift_ = g.Key.shift_,
                    no_machine = g.Key.no_machine,
                    mc_area = g.Key.mc_area,
                    sch = g.Sum(x => x.sch), // sum target
                    total_actoutput = g.Sum(x => x.total_actoutput),
                    total_output_pcs = g.Sum(x => x.total_output_pcs),
                    pn_stdoutput = g.Sum(x => x.pn_stdoutput),
                    totalRJ = g.Sum(x => x.totalRJ), // sum reject
                    totalDT = g.Sum(x => x.totalDT),
                    totalNotRun = g.Sum(x => x.totalNotRun),
                })
                .OrderBy(x => x.date_).ThenBy(x => x.shift_).ThenBy(x => x.mc_area)
                .ToList()
                .Select(x => new VO_MAIN_CALC // Assuming you have a class named VO_MAIN_CALC for the result
                {
                    we_ = x.we_,
                    date_ = x.date_,
                    shift_ = x.shift_,
                    no_machine = x.no_machine,
                    mc_area = x.mc_area,
                    sch = x.sch,
                    total_actoutput = x.total_actoutput,
                    total_output_pcs = x.total_output_pcs,
                    pn_stdoutput = x.pn_stdoutput,
                    totalRJ = x.totalRJ,
                    totalDT = x.totalDT,
                    totalNotRun = x.totalNotRun
                })
                .ToList(),

                // >>>>> OUTPUT REJECT dijumlah berdasarkan PPM jika MC, PN, dan Remarks sama
                VO_REJECT_PPs = ssc_db.VO_REJECT_PP.Where(x => x.date_ >= startDate && x.date_ <= finishDate && x.mold != null && x.pn_ct != null && x.pn_cav != null && x.part_pp != null && x.op_kpk != null && x.op_kpk != "00220" && x.total_actoutput != null && x.total_actoutput > 0).
                GroupBy(x => new {  x.date_, x.shift_, x.mc_area, x.no_machine, x.mold, x.partnumber,x.DESC, x.pn_ct,  x.pn_cav, x.sch, x.total_actoutput, x.total_output_pcs,  x.reject_qty,  x.reject_code,  x.rj_remarks })
                .Select(g => new
                {
                    date_ = g.Key.date_,
                    shift_ = g.Key.shift_,
                    mc_area = g.Key.mc_area,
                    no_machine = g.Key.no_machine,
                    partnumber = g.Key.partnumber,
                    mold = g.Key.mold,
                    DESC = g.Key.DESC,
                    pn_ct = g.Key.pn_ct,
                    pn_cav = g.Key.pn_cav,
                    sch = g.Key.sch,
                    total_actoutput = g.FirstOrDefault().total_actoutput,
                    total_output_pcs = g.Key.total_output_pcs,
                    reject_qty = g.Sum(x => x.reject_qty),
                    reject_code = g.Key.reject_code,
                    rj_remarks = g.Key.rj_remarks,
                    //rj_rate2 = Math.Round(((decimal)g.Sum(x => x.reject_qty) / (decimal)g.FirstOrDefault().total_actoutput) * 100, 2)

                })
                .OrderBy(x => x.date_).ThenBy(x => x.shift_).ThenBy(x => x.no_machine).ThenBy(x => x.reject_code).ToList()
                .Select(x => new VO_REJECT_PP
                {
                    date_ = x.date_,
                    shift_ = x.shift_,
                    mc_area = x.mc_area,
                    no_machine = x.no_machine,
                    partnumber = x.partnumber,
                    mold = x.mold,
                    DESC = x.DESC,
                    pn_ct = x.pn_ct,
                    pn_cav = x.pn_cav,
                    sch = x.sch,
                    total_actoutput = x.total_actoutput,
                    total_output_pcs = x.total_output_pcs,
                    reject_qty = x.reject_qty,
                    reject_code = x.reject_code,
                    rj_remarks = x.rj_remarks,
                }).ToList(),


            };

            // ------------------------------------- TAMPILKAN completeness ------------------------------------- //
            // Default value
            decimal completeness = 0;
            decimal mc = 0;
            //ViewBag.VO_MAIN_CALC_AREAs = VO_MAIN_CALC_AREAs;
            foreach (var item in table_data.VO_MAIN_CALC_AREAs)
            {
                decimal downtime = item.totalDT.GetValueOrDefault();

                if (item.sch != null && item.totalRJ != null && item.totalDT != null && item.pn_stdoutput != null && item.sch != 0)
                {
                    completeness = Math.Round((((decimal)item.total_output_pcs.GetValueOrDefault() + (decimal)item.totalRJ.GetValueOrDefault()) + (downtime * (decimal)item.pn_stdoutput.GetValueOrDefault())) / (decimal)item.sch.GetValueOrDefault(), 2);
                }

                //completeness += completeness;
                if (completeness > 90)
                {
                    mc += mc;
                }
            }

            // hitung rata-rata completeness
            if (table_data.VO_MAIN_CALC_AREAs.Count() > 0)
            {
                ViewBag.completeness = completeness;
                ViewBag.mc = mc;
            }
            // ------------------------------------- TAMPILKAN RATA-RATA TOTAL OEE ------------------------------------- //

            decimal totalAVAILABILITY = 0;
            decimal totalPERFORMANCE = 0;
            decimal totalQUALITY = 0;

            decimal totalOEE_SCORE = 0;
            foreach (var pimUserOutput in table_data.VO_MAIN_CALCs)
            {
                decimal availability = 0, performance = 0, quality = 0;
                decimal availability_SCORE = 0, performance_SCORE = 0, quality_SCORE = 0;
                decimal OEE_SCORE = 0;

                // set performance to some default value or handle the error appropriately
                if (pimUserOutput.pn_operatingtime.HasValue && pimUserOutput.total_time.HasValue && pimUserOutput.pn_operatingtime.Value != 0 && pimUserOutput.total_time.Value != 0)
                {
                    availability = Math.Round((((decimal)pimUserOutput.pn_operatingtime.Value) / (decimal)pimUserOutput.total_time.Value), 2);
                }
                else
                {
                    availability = 0;
                }
                if (pimUserOutput.total_output_pcs.HasValue && pimUserOutput.pn_stdoutput.HasValue && pimUserOutput.total_output_pcs.Value != 0 && pimUserOutput.pn_stdoutput.Value != 0)
                {
                    performance = Math.Round(((decimal)pimUserOutput.total_output_pcs.Value / (decimal)pimUserOutput.pn_stdoutput.Value), 2);
                }
                else
                {
                    performance = 0;
                }
                if (pimUserOutput.total_good_output.HasValue && pimUserOutput.total_output_pcs.HasValue && pimUserOutput.total_good_output.Value != 0 && pimUserOutput.total_output_pcs.Value != 0)
                {
                    quality = Math.Round(((decimal)pimUserOutput.total_good_output.Value / (decimal)pimUserOutput.total_output_pcs.Value), 2);
                }
                else
                {
                    quality = 0;
                }


                // Jika performance lebih dari 1.2, maka setting == MAX 1.2 

                //if (performance >= 1.2m)
                //{
                //    performance = 1.2m;
                //}


                availability_SCORE = Math.Round((availability) * 100, 2);
                performance_SCORE = Math.Round((performance) * 100, 2);
                quality_SCORE = Math.Round((quality) * 100, 2);
                OEE_SCORE = Math.Round((availability * performance * quality) * 100, 2);

                totalAVAILABILITY += availability_SCORE;
                totalPERFORMANCE += performance_SCORE;
                totalQUALITY += quality_SCORE;

                totalOEE_SCORE += OEE_SCORE;
            }

            // hitung rata-rata OEE_SCORE
            if (table_data.VO_MAIN_CALCs.Count() > 0)
            {
                decimal AvgAVAILABILITY_SCORE = Math.Round((totalAVAILABILITY / (decimal)table_data.VO_MAIN_CALCs.Count()), 2);
                ViewBag.AvgAVAILABILITY_SCORE = AvgAVAILABILITY_SCORE;
                decimal AvgPERFORMAMCE_SCORE = Math.Round((totalPERFORMANCE / (decimal)table_data.VO_MAIN_CALCs.Count()), 2);
                ViewBag.AvgPERFORMAMCE_SCORE = AvgPERFORMAMCE_SCORE;
                decimal AvgQUALITY_SCORE = Math.Round((totalQUALITY / (decimal)table_data.VO_MAIN_CALCs.Count()), 2);
                ViewBag.AvgQUALITY_SCORE = AvgQUALITY_SCORE;
                decimal apqOEE_SCORE = Math.Round(((AvgAVAILABILITY_SCORE * AvgPERFORMAMCE_SCORE * AvgQUALITY_SCORE) / 10000), 2);
                ViewBag.apqOEE_SCORE = apqOEE_SCORE;
                decimal AvgOEE_SCORE = Math.Round((totalOEE_SCORE / (decimal)table_data.VO_MAIN_CALCs.Count()), 2);
                ViewBag.AvgOEE_SCORE = AvgOEE_SCORE;
            }
            // jika ada data yang ditemukan, tampilkan data tersebut
            // jika tidak ada data yang ditemukan, tampilkan error404
            if (table_data != null && startDate != null && finishDate != null)
            {
                return View(table_data);
            }
            else
            {
                return View("error404");
            }
        }

        // filter data OEE berdasarkan shift dan date
        public ActionResult filterDownloadData(DateTime? dateStart = null, DateTime? dateFinish = null)
        {

            DateTime? dateFilter = (DateTime?)System.Web.HttpContext.Current.Session["myDate"];
            DateTime filterDate = dateFilter ?? DateTime.Now.Date;
            ViewBag.FilterDate = filterDate;

            DateTime now = DateTime.Now;
            DateTime startDate = dateStart ?? now.Date;
            DateTime finishDate = dateFinish ?? now.Date;
            // ---------------------------------------------- //
            int inputShiftNow = 0;
            if (now.TimeOfDay >= new TimeSpan(7, 10, 0) && now.TimeOfDay < new TimeSpan(15, 40, 0))
            {
                inputShiftNow = 2;
            }
            else if (now.TimeOfDay >= new TimeSpan(15, 40, 0) && now.TimeOfDay < new TimeSpan(22, 40, 0))
            {
                inputShiftNow = 3;
            }
            else
            {
                inputShiftNow = 1;
                dateFinish = now.AddDays(1).Date;
            }

            ViewBag.DateNow = now;
            ViewBag.inputShiftNow = inputShiftNow;

            ViewBag.startDate = startDate;
            ViewBag.finishDate = finishDate;

            // ---------------------------------------------- //


            var table_data = new pimListData
            {
                VO_MAIN_CALCs = ssc_db.VO_MAIN_CALC.Where(x => x.date_ >= startDate && x.date_ <= finishDate && x.mold != null && x.op_kpk != null && x.op_kpk != "00220").
                    OrderBy(x => x.date_).ThenBy(x => x.shift_).ThenBy(x => x.no_machine).ToList(),

                // read data - relationship table downtime with user, select kecuali kode : PD09 (not running bukan downtime)
                VO_DOWNTIMEs = ssc_db.VO_DOWNTIME.Where(x => x.date_ >= startDate && x.date_ <= finishDate && x.mold != null && x.op_kpk != null && x.op_kpk != "00220" && x.downtime_code != "PD09").
                    OrderBy(x => x.date_).ThenBy(x => x.shift_).ThenBy(x => x.no_machine).ThenBy(x => x.dt_start).ToList(),

                // >>>>> OUTPUT REJECT dijumlah berdasarkan PPM jika MC, PN, dan Remarks sama
                VO_REJECTs = ssc_db.VO_REJECT.Where(x => x.date_ >= startDate && x.date_ <= finishDate && x.mold != null && x.op_kpk != null && x.op_kpk != "00220" && x.total_actoutput != null && x.total_actoutput > 0).
                GroupBy(x => new { x.we_, x.date_, x.shift_, x.no_machine, x.partnumber, x.reject_code, x.rj_remarks })
                .Select(g => new
                {
                    we_ = g.Key.we_,
                    date_ = g.Key.date_,
                    shift_ = g.Key.shift_,
                    no_machine = g.Key.no_machine,
                    partnumber = g.Key.partnumber,
                    reject_code = g.Key.reject_code,
                    rj_remarks = g.Key.rj_remarks,
                    reject_qty = g.Sum(x => x.reject_qty),
                    total_actoutput = g.FirstOrDefault().total_actoutput
                    //rj_rate2 = Math.Round(((decimal)g.Sum(x => x.reject_qty) / (decimal)g.FirstOrDefault().total_actoutput) * 100, 2)

                })
                .OrderBy(x => x.date_).ThenBy(x => x.shift_).ThenBy(x => x.no_machine).ThenBy(x => x.reject_code).ToList()
                .Select(x => new VO_REJECT
                {
                    we_ = x.we_,
                    date_ = x.date_,
                    shift_ = x.shift_,
                    no_machine = x.no_machine,
                    partnumber = x.partnumber,
                    reject_code = x.reject_code,
                    rj_remarks = x.rj_remarks,
                    reject_qty = x.reject_qty,
                }).ToList(),

                // Menampilkan Data OEE
                VO_MAIN_CALC_TOPs = ssc_db.VO_MAIN_CALC.Where(x => x.date_ >= startDate && x.date_ <= finishDate && x.total_time != null && x.mold != null && x.op_kpk != null && x.op_kpk != "00220" && x.total_time > 0)
                .GroupBy(x => new { x.we_, x.date_, x.shift_, x.no_machine })
                .Select(g => new
                {
                    we_ = g.Key.we_,
                    date_ = g.Key.date_,
                    shift_ = g.Key.shift_,
                    no_machine = g.Key.no_machine,
                    total_time = g.Sum(x => x.total_time),
                    total_good_output = g.Sum(x => x.total_good_output),
                    total_actoutput = g.Sum(x => x.total_actoutput),
                    total_output_pcs = g.Sum(x => x.total_output_pcs),
                    pn_stdoutput = g.Sum(x => x.pn_stdoutput),
                    pn_operatingtime = g.Sum(x => x.pn_operatingtime),
                    totalRJ = g.Sum(x => x.totalRJ),
                    totalDT = g.Sum(x => x.totalDT),
                    totalNotRun = g.Sum(x => x.totalNotRun),
                    performance = (g.Sum(x => x.pn_stdoutput) == null || g.Sum(x => x.pn_stdoutput) == 0) ? 0 : Math.Round(((decimal)(g.Sum(x => x.total_output_pcs) ?? 0) / (decimal)g.Sum(x => x.pn_stdoutput.Value)) * 100, 2)
                }).
                OrderBy(x => x.date_).ThenBy(x => x.shift_).ThenBy(x => x.no_machine).ToList()
                .Select(x => new VO_MAIN_CALC
                {
                    we_ = x.we_,
                    date_ = x.date_,
                    shift_ = x.shift_,
                    no_machine = x.no_machine,
                    total_time = x.total_time,
                    total_good_output = x.total_good_output,
                    total_actoutput = x.total_actoutput,
                    total_output_pcs = x.total_output_pcs,
                    pn_stdoutput = x.pn_stdoutput,
                    pn_operatingtime = x.pn_operatingtime,
                    totalRJ = x.totalRJ,
                    totalDT = x.totalDT,
                    totalNotRun = x.totalNotRun
                }).ToList(),

                // CALCULATE COMPLETENESS PER AREA
                VO_MAIN_CALC_AREAs = ssc_db.VO_MAIN_CALC
                .Where(x => x.date_ >= startDate && x.date_ <= finishDate && x.total_time != null &&
                            x.mold != null && x.op_kpk != null && x.op_kpk != "00220" && x.total_time > 0)
                .GroupBy(x => new { x.we_, x.date_, x.shift_, x.no_machine, x.mc_area })
                .Select(g => new
                {
                    we_ = g.Key.we_,
                    date_ = g.Key.date_,
                    shift_ = g.Key.shift_,
                    no_machine = g.Key.no_machine,
                    mc_area = g.Key.mc_area,
                    sch = g.Sum(x => x.sch), // sum target
                    total_actoutput = g.Sum(x => x.total_actoutput),
                    total_output_pcs = g.Sum(x => x.total_output_pcs),
                    pn_stdoutput = g.Sum(x => x.pn_stdoutput),
                    totalRJ = g.Sum(x => x.totalRJ), // sum reject
                    totalDT = g.Sum(x => x.totalDT),
                    totalNotRun = g.Sum(x => x.totalNotRun),
                })
                .OrderBy(x => x.date_).ThenBy(x => x.shift_).ThenBy(x => x.mc_area)
                .ToList()
                .Select(x => new VO_MAIN_CALC // Assuming you have a class named VO_MAIN_CALC for the result
                {
                    we_ = x.we_,
                    date_ = x.date_,
                    shift_ = x.shift_,
                    no_machine = x.no_machine,
                    mc_area = x.mc_area,
                    sch = x.sch,
                    total_actoutput = x.total_actoutput,
                    total_output_pcs = x.total_output_pcs,
                    pn_stdoutput = x.pn_stdoutput,
                    totalRJ = x.totalRJ,
                    totalDT = x.totalDT,
                    totalNotRun = x.totalNotRun
                })
                .ToList(),

                // >>>>> OUTPUT REJECT PP
                VO_REJECT_PPs = ssc_db.VO_REJECT_PP.Where(x => x.date_ >= startDate && x.date_ <= finishDate && x.mold != null && x.pn_ct != null && x.pn_cav != null && x.part_pp != null && x.op_kpk != null && x.op_kpk != "00220" && x.total_actoutput != null && x.total_actoutput > 0).
                GroupBy(x => new { x.date_, x.shift_, x.mc_area, x.no_machine, x.mold, x.partnumber, x.DESC, x.pn_ct, x.pn_cav, x.sch, x.total_actoutput, x.total_output_pcs, x.reject_qty, x.reject_code, x.rj_remarks })
                .Select(g => new
                {
                    date_ = g.Key.date_,
                    shift_ = g.Key.shift_,
                    mc_area = g.Key.mc_area,
                    no_machine = g.Key.no_machine,
                    partnumber = g.Key.partnumber,
                    mold = g.Key.mold,
                    DESC = g.Key.DESC,
                    pn_ct = g.Key.pn_ct,
                    pn_cav = g.Key.pn_cav,
                    sch = g.Key.sch,
                    total_actoutput = g.FirstOrDefault().total_actoutput,
                    total_output_pcs = g.Key.total_output_pcs,
                    reject_qty = g.Sum(x => x.reject_qty),
                    reject_code = g.Key.reject_code,
                    rj_remarks = g.Key.rj_remarks,
                    //rj_rate2 = Math.Round(((decimal)g.Sum(x => x.reject_qty) / (decimal)g.FirstOrDefault().total_actoutput) * 100, 2)

                })
                .OrderBy(x => x.date_).ThenBy(x => x.shift_).ThenBy(x => x.no_machine).ThenBy(x => x.reject_code).ToList()
                .Select(x => new VO_REJECT_PP
                {
                    date_ = x.date_,
                    shift_ = x.shift_,
                    mc_area = x.mc_area,
                    no_machine = x.no_machine,
                    partnumber = x.partnumber,
                    mold = x.mold,
                    DESC = x.DESC,
                    pn_ct = x.pn_ct,
                    pn_cav = x.pn_cav,
                    sch = x.sch,
                    total_actoutput = x.total_actoutput,
                    total_output_pcs = x.total_output_pcs,
                    reject_qty = x.reject_qty,
                    reject_code = x.reject_code,
                    rj_remarks = x.rj_remarks,
                }).ToList(),

            };


            // ------------------------------------- TAMPILKAN RATA-RATA TOTAL OEE ------------------------------------- //

            decimal totalAVAILABILITY = 0;
            decimal totalPERFORMANCE = 0;
            decimal totalQUALITY = 0;

            decimal totalOEE_SCORE = 0;
            foreach (var pimUserOutput in table_data.VO_MAIN_CALCs)
            {
                decimal availability = 0, performance = 0, quality = 0;
                decimal availability_SCORE = 0, performance_SCORE = 0, quality_SCORE = 0;
                decimal OEE_SCORE = 0;

                // set performance to some default value or handle the error appropriately

                // AVAILABIILTY
                if (pimUserOutput.pn_operatingtime.HasValue && pimUserOutput.total_time.HasValue && pimUserOutput.pn_operatingtime.Value != 0 && pimUserOutput.total_time.Value != 0)
                {
                    availability = Math.Round((((decimal)pimUserOutput.pn_operatingtime.Value) / (decimal)pimUserOutput.total_time.Value), 2);
                }
                else
                {
                    availability = 0;
                }

                // PERFORMANCE
                if (pimUserOutput.total_output_pcs.HasValue && pimUserOutput.pn_stdoutput.HasValue && pimUserOutput.total_output_pcs.Value != 0 && pimUserOutput.pn_stdoutput.Value != 0)
                {
                    performance = Math.Round(((decimal)pimUserOutput.total_output_pcs.Value / (decimal)pimUserOutput.pn_stdoutput.Value), 2);
                }
                else
                {
                    performance = 0;
                }


                // QUALITY
                if (pimUserOutput.total_good_output.HasValue && pimUserOutput.total_output_pcs.HasValue && pimUserOutput.total_good_output.Value != 0 && pimUserOutput.total_output_pcs.Value != 0)
                {
                    quality = Math.Round(((decimal)pimUserOutput.total_good_output.Value / (decimal)pimUserOutput.total_output_pcs.Value), 2);
                }
                else
                {
                    quality = 0;
                }

                // Jika performance lebih dari 1.2, maka setting == MAX 1.2 

                if (performance >= 1.2m)
                {
                    performance = 1.2m;
                }


                //availability_SCORE = Math.Round((availability) * 100, 2);
                //performance_SCORE = Math.Round((performance) * 100, 2);
                //quality_SCORE = Math.Round((quality) * 100, 2);
                //OEE_SCORE = Math.Round((availability * performance * quality) * 100, 2);

                availability_SCORE = (availability) * 100;
                performance_SCORE = (performance) * 100;
                quality_SCORE = (quality) * 100;
                OEE_SCORE = (availability * performance * quality) * 100;

                totalAVAILABILITY += availability_SCORE;
                totalPERFORMANCE += performance_SCORE;
                totalQUALITY += quality_SCORE;

                totalOEE_SCORE += OEE_SCORE;
            }

            // hitung rata-rata OEE_SCORE
            if (table_data.VO_MAIN_CALCs.Count() > 0)
            {
                decimal AvgAVAILABILITY_SCORE = Math.Round((totalAVAILABILITY / (decimal)table_data.VO_MAIN_CALCs.Count()), 2);
                ViewBag.AvgAVAILABILITY_SCORE = AvgAVAILABILITY_SCORE;
                decimal AvgPERFORMAMCE_SCORE = Math.Round((totalPERFORMANCE / (decimal)table_data.VO_MAIN_CALCs.Count()), 2);
                ViewBag.AvgPERFORMAMCE_SCORE = AvgPERFORMAMCE_SCORE;
                decimal AvgQUALITY_SCORE = Math.Round((totalQUALITY / (decimal)table_data.VO_MAIN_CALCs.Count()), 2);
                ViewBag.AvgQUALITY_SCORE = AvgQUALITY_SCORE;
                //decimal apqOEE_SCORE = Math.Round(((AvgAVAILABILITY_SCORE * AvgPERFORMAMCE_SCORE * AvgQUALITY_SCORE) / 10000), 2);
                //ViewBag.apqOEE_SCORE = apqOEE_SCORE;
                decimal AvgOEE_SCORE = Math.Round((totalOEE_SCORE / (decimal)table_data.VO_MAIN_CALCs.Count()), 2);
                ViewBag.AvgOEE_SCORE = AvgOEE_SCORE;
            }
            // jika ada data yang ditemukan, tampilkan data tersebut
            if (table_data != null && startDate != null && finishDate != null)
            {
                return View("displayData", table_data);
            }
            // jika tidak ada data yang ditemukan, tampilkan error404
            else
            {
                return View("error404");
            }

        }

        // filter data OEE berdasarkan shift dan date
        public ActionResult filterAbnormalityData(DateTime? dateStart = null, DateTime? dateFinish = null)
        {

            DateTime? dateFilter = (DateTime?)System.Web.HttpContext.Current.Session["myDate"];
            DateTime filterDate = dateFilter ?? DateTime.Now.Date;
            ViewBag.FilterDate = filterDate;

            DateTime now = DateTime.Now;
            DateTime startDate = dateStart ?? now.Date;
            DateTime finishDate = dateFinish ?? now.Date;
            // ---------------------------------------------- //
            int inputShiftNow = 0;
            if (now.TimeOfDay >= new TimeSpan(7, 10, 0) && now.TimeOfDay < new TimeSpan(15, 40, 0))
            {
                inputShiftNow = 2;
            }
            else if (now.TimeOfDay >= new TimeSpan(15, 40, 0) && now.TimeOfDay < new TimeSpan(22, 40, 0))
            {
                inputShiftNow = 3;
            }
            else
            {
                inputShiftNow = 1;
                dateFinish = now.AddDays(1).Date;
            }

            ViewBag.DateNow = now;
            ViewBag.inputShiftNow = inputShiftNow;

            ViewBag.startDate = startDate;
            ViewBag.finishDate = finishDate;

            ViewBag.abnormality = "Yes";

            // ---------------------------------------------- //

            var table_data = new pimListData
            {

                // database user output select data yang teriisi dan yang kpk nya bukan kpk Diana Testing hehe...
                VO_MAIN_CALCs = ssc_db.VO_MAIN_CALC.Where(x => x.date_ >= startDate && x.date_ <= finishDate && x.mold != null && x.op_kpk != null && x.op_kpk != "00220").
                OrderBy(x => x.date_).ThenBy(x => x.shift_).ThenBy(x => x.no_machine).ToList(),

                // read data - relationship table downtime with user, select kecuali kode : PD09 (not running bukan downtime)
                VO_DOWNTIMEs = ssc_db.VO_DOWNTIME.Where(x => x.date_ >= startDate && x.date_ <= finishDate && x.mold != null && x.op_kpk != null && x.op_kpk != "00220" && x.downtime_code != "PD09").
                    OrderBy(x => x.date_).ThenBy(x => x.shift_).ThenBy(x => x.no_machine).ThenBy(x => x.dt_start).ToList(),

                // >>>>> OUTPUT REJECT dijumlah berdasarkan PPM jika MC, PN, dan Remarks sama
                VO_REJECTs = ssc_db.VO_REJECT.Where(x => x.date_ >= startDate && x.date_ <= finishDate && x.mold != null && x.op_kpk != null && x.op_kpk != "00220" && x.total_actoutput != null && x.total_actoutput > 0).
                GroupBy(x => new { x.we_, x.date_, x.shift_, x.no_machine, x.partnumber, x.reject_code, x.rj_remarks })
                .Select(g => new
                {
                    we_ = g.Key.we_,
                    date_ = g.Key.date_,
                    shift_ = g.Key.shift_,
                    no_machine = g.Key.no_machine,
                    partnumber = g.Key.partnumber,
                    reject_code = g.Key.reject_code,
                    rj_remarks = g.Key.rj_remarks,
                    reject_qty = g.Sum(x => x.reject_qty),
                    total_actoutput = g.FirstOrDefault().total_actoutput
                    //rj_rate2 = Math.Round(((decimal)g.Sum(x => x.reject_qty) / (decimal)g.FirstOrDefault().total_actoutput) * 100, 2)

                })
                .OrderBy(x => x.date_).ThenBy(x => x.shift_).ThenBy(x => x.no_machine).ThenBy(x => x.reject_code).ToList()
                .Select(x => new VO_REJECT
                {
                    we_ = x.we_,
                    date_ = x.date_,
                    shift_ = x.shift_,
                    no_machine = x.no_machine,
                    partnumber = x.partnumber,
                    reject_code = x.reject_code,
                    rj_remarks = x.rj_remarks,
                    reject_qty = x.reject_qty,
                }).ToList(),

                // Menampilkan Data OEE
                VO_MAIN_CALC_TOPs = ssc_db.VO_MAIN_CALC.Where(x => x.date_ >= startDate && x.date_ <= finishDate && x.total_time != null && x.mold != null && x.op_kpk != null && x.op_kpk != "00220" && x.total_time > 0)
                .GroupBy(x => new { x.we_, x.date_, x.shift_, x.no_machine })
                .Select(g => new
                {
                    we_ = g.Key.we_,
                    date_ = g.Key.date_,
                    shift_ = g.Key.shift_,
                    no_machine = g.Key.no_machine,
                    total_time = g.Sum(x => x.total_time),
                    total_good_output = g.Sum(x => x.total_good_output),
                    total_actoutput = g.Sum(x => x.total_actoutput),
                    total_output_pcs = g.Sum(x => x.total_output_pcs),
                    pn_stdoutput = g.Sum(x => x.pn_stdoutput),
                    pn_operatingtime = g.Sum(x => x.pn_operatingtime),
                    totalRJ = g.Sum(x => x.totalRJ),
                    totalDT = g.Sum(x => x.totalDT),
                    totalNotRun = g.Sum(x => x.totalNotRun),
                    performance = (g.Sum(x => x.pn_stdoutput) == null || g.Sum(x => x.pn_stdoutput) == 0) ? 0 : Math.Round(((decimal)(g.Sum(x => x.total_output_pcs) ?? 0) / (decimal)g.Sum(x => x.pn_stdoutput.Value)) * 100, 2)
                }).
                OrderBy(x => x.date_).ThenBy(x => x.shift_).ThenBy(x => x.no_machine).ToList()
                .Select(x => new VO_MAIN_CALC
                {
                    we_ = x.we_,
                    date_ = x.date_,
                    shift_ = x.shift_,
                    no_machine = x.no_machine,
                    total_time = x.total_time,
                    total_good_output = x.total_good_output,
                    total_actoutput = x.total_actoutput,
                    total_output_pcs = x.total_output_pcs,
                    pn_stdoutput = x.pn_stdoutput,
                    pn_operatingtime = x.pn_operatingtime,
                    totalRJ = x.totalRJ,
                    totalDT = x.totalDT,
                    totalNotRun = x.totalNotRun
                }).ToList(),

                // CALCULATE COMPLETENESS PER AREA
                VO_MAIN_CALC_AREAs = ssc_db.VO_MAIN_CALC
                .Where(x => x.date_ >= startDate && x.date_ <= finishDate && x.total_time != null &&
                            x.mold != null && x.op_kpk != null && x.op_kpk != "00220" && x.total_time > 0)
                .GroupBy(x => new { x.we_, x.date_, x.shift_, x.no_machine, x.mc_area })
                .Select(g => new
                {
                    we_ = g.Key.we_,
                    date_ = g.Key.date_,
                    shift_ = g.Key.shift_,
                    no_machine = g.Key.no_machine,
                    mc_area = g.Key.mc_area,
                    sch = g.Sum(x => x.sch), // sum target
                    total_actoutput = g.Sum(x => x.total_actoutput),
                    total_output_pcs = g.Sum(x => x.total_output_pcs),
                    pn_stdoutput = g.Sum(x => x.pn_stdoutput),
                    totalRJ = g.Sum(x => x.totalRJ), // sum reject
                    totalDT = (int?)Math.Round(((decimal)(g.Sum(x => x.totalDT) ?? 0) / 60)), // Cast to int? // bagi per 60 menit
                    totalNotRun = g.Sum(x => x.totalNotRun),
                })
                .OrderBy(x => x.date_).ThenBy(x => x.shift_).ThenBy(x => x.mc_area)
                .ToList()
                .Select(x => new VO_MAIN_CALC // Assuming you have a class named VO_MAIN_CALC for the result
                {
                    we_ = x.we_,
                    date_ = x.date_,
                    shift_ = x.shift_,
                    no_machine = x.no_machine,
                    mc_area = x.mc_area,
                    sch = x.sch,
                    total_actoutput = x.total_actoutput,
                    total_output_pcs = x.total_output_pcs,
                    pn_stdoutput = x.pn_stdoutput,
                    totalRJ = x.totalRJ,
                    totalDT = x.totalDT,
                    totalNotRun = x.totalNotRun
                })
                .ToList(),

                // >>>>> OUTPUT REJECT PP
                VO_REJECT_PPs = ssc_db.VO_REJECT_PP.Where(x => x.date_ >= startDate && x.date_ <= finishDate && x.mold != null && x.pn_ct != null && x.pn_cav != null && x.part_pp != null && x.op_kpk != null && x.op_kpk != "00220" && x.total_actoutput != null && x.total_actoutput > 0).
                GroupBy(x => new { x.date_, x.shift_, x.mc_area, x.no_machine, x.mold, x.partnumber, x.DESC, x.pn_ct, x.pn_cav, x.sch, x.total_actoutput, x.total_output_pcs, x.reject_qty, x.reject_code, x.rj_remarks })
                .Select(g => new
                {
                    date_ = g.Key.date_,
                    shift_ = g.Key.shift_,
                    mc_area = g.Key.mc_area,
                    no_machine = g.Key.no_machine,
                    partnumber = g.Key.partnumber,
                    mold = g.Key.mold,
                    DESC = g.Key.DESC,
                    pn_ct = g.Key.pn_ct,
                    pn_cav = g.Key.pn_cav,
                    sch = g.Key.sch,
                    total_actoutput = g.FirstOrDefault().total_actoutput,
                    total_output_pcs = g.Key.total_output_pcs,
                    reject_qty = g.Sum(x => x.reject_qty),
                    reject_code = g.Key.reject_code,
                    rj_remarks = g.Key.rj_remarks,
                    //rj_rate2 = Math.Round(((decimal)g.Sum(x => x.reject_qty) / (decimal)g.FirstOrDefault().total_actoutput) * 100, 2)

                })
                .OrderBy(x => x.date_).ThenBy(x => x.shift_).ThenBy(x => x.no_machine).ThenBy(x => x.reject_code).ToList()
                .Select(x => new VO_REJECT_PP
                {
                    date_ = x.date_,
                    shift_ = x.shift_,
                    mc_area = x.mc_area,
                    no_machine = x.no_machine,
                    partnumber = x.partnumber,
                    mold = x.mold,
                    DESC = x.DESC,
                    pn_ct = x.pn_ct,
                    pn_cav = x.pn_cav,
                    sch = x.sch,
                    total_actoutput = x.total_actoutput,
                    total_output_pcs = x.total_output_pcs,
                    reject_qty = x.reject_qty,
                    reject_code = x.reject_code,
                    rj_remarks = x.rj_remarks,
                }).ToList(),
            };


            // jika availability atau performance atau quality memiliki nilai kurang dari nol maka:
            // >>> tampilkan data
            // ...

            var abnormality_output = table_data.VO_MAIN_CALCs
                 .Where(x => (x.total_time > 510 || (x.pn_operatingtime.HasValue && x.pn_operatingtime != 0 && x.total_time.HasValue && x.total_time != 0 && Math.Round(((decimal)x.pn_operatingtime.Value) / x.total_time.Value, 2) < 0)) || // Availability, Performance, Quality < 0
                             (x.total_output_pcs.HasValue && x.total_output_pcs != 0 && x.pn_stdoutput.HasValue && x.pn_stdoutput != 0 && Math.Round(((double)x.total_output_pcs.Value / x.pn_stdoutput.Value) * 100, 2) > 120) || // Performance max 120
                             (x.total_output_pcs.HasValue && x.total_output_pcs != 0 && x.pn_stdoutput.HasValue && x.pn_stdoutput != 0 && Math.Round(((double)x.total_output_pcs.Value / x.pn_stdoutput.Value) * 100, 2) < 70) || // Performance min 70
                             (x.total_output_pcs == null || x.total_output_pcs == 0 && x.pn_stdoutput == null || x.pn_stdoutput == 0) || // Performance sama dengan 0
                             (x.total_output_pcs.HasValue && x.total_output_pcs != 0 && x.pn_stdoutput.HasValue && x.pn_stdoutput != 0 && (x.pn_stdoutput.Value - x.total_output_pcs.Value) > (x.pn_stdoutput.Value * 1/5) ) || // Variance lebih dari 20% Math.Round(((double)x.pn_stdoutput.Value * (20/100)) * 100, 2)
                             (x.total_good_output.HasValue && x.total_good_output != 0 && x.total_output_pcs.HasValue && x.total_output_pcs != 0 && Math.Round(((double)x.total_good_output.Value) / x.total_output_pcs.Value, 2) > 1)) // Quality

                 .ToList();

            // ---------------------------------------------- //

            var table_data_abnormal = new pimListData
            {
                // database user output select data yang teriisi dan yang kpk nya bukan kpk Diana Testing hehe...
                VO_MAIN_CALCs = abnormality_output.Where(x => x.date_ >= startDate && x.date_ <= finishDate && x.mold != null && x.op_kpk != null && x.op_kpk != "00220").
                    OrderBy(x => x.date_).ThenBy(x => x.shift_).ThenBy(x => x.no_machine).ToList(),

                // read data - relationship table downtime with user, select kecuali kode : PD09 (not running bukan downtime)
                VO_DOWNTIMEs = ssc_db.VO_DOWNTIME.Where(x => x.date_ >= startDate && x.date_ <= finishDate).
                    OrderBy(x => x.date_).ThenBy(x => x.shift_).ThenBy(x => x.no_machine).ThenBy(x => x.dt_start).ToList(),

                // read data - relationship table reject with user 
                VO_REJECTs = ssc_db.VO_REJECT.Where(x => x.date_ >= startDate && x.date_ <= finishDate && x.total_actoutput != null && x.total_actoutput > 0).
                    OrderBy(x => x.date_).ThenBy(x => x.shift_).ThenBy(x => x.no_machine).ThenBy(x => x.reject_code).ToList(),

                // read data - relationship table reject with user 
                VO_REJECT_PPs = ssc_db.VO_REJECT_PP.Where(x => x.date_ >= startDate && x.date_ <= finishDate && x.mold != null && x.pn_ct != null && x.pn_cav != null && x.part_pp != null && x.op_kpk != null && x.op_kpk != "00220" && x.total_actoutput != null && x.total_actoutput > 0).
                    OrderBy(x => x.date_).ThenBy(x => x.shift_).ThenBy(x => x.no_machine).ThenBy(x => x.reject_code).ToList(),

                // Menampilkan Data OEE
                VO_MAIN_CALC_TOPs = abnormality_output.Where(x => (x.date_ >= startDate && x.date_ <= finishDate && x.mold != null && x.op_kpk != null && x.op_kpk != "00220") || x.total_time > 510).
                    OrderBy(x => x.date_).ThenBy(x => x.shift_).ThenBy(x => x.no_machine).ToList(),

            };

            // jika ada data yang ditemukan, tampilkan data tersebut
            if (table_data_abnormal != null)
            {
                return View("displayData", table_data_abnormal);
            }
            // jika tidak ada data yang ditemukan, tampilkan error404
            else
            {
                return View("error404");
            }

        }

        // DOWNLOAD DATA OEE - DOWNLOAD FOR POWER BI
        [HttpGet]
        public ActionResult exportOutputOEE_PBI(DateTime? dateStart = null, DateTime? dateEnd = null)
        {
            //========================================================//

            DateTime now = DateTime.Now;
            DateTime startDate = dateStart ?? now.Date;
            DateTime finishDate = dateEnd ?? now.Date;

            ViewBag.DateNow = now;
            ViewBag.startDate = startDate;
            ViewBag.finishDate = finishDate;

            // Declare Variable Data Output
            var outputQuery = ssc_db.VO_MAIN_CALC
                .Where(x => x.date_ >= startDate && x.date_ <= finishDate
                       && x.mold != null && x.op_kpk != null && x.op_kpk != "00220" && x.total_time > 0);

            var rejectQuery = ssc_db.VO_REJECT
                .Where(x => x.date_ >= startDate && x.date_ <= finishDate
                       && x.mold != null && x.op_kpk != null && x.op_kpk != "00220" && x.total_actoutput != null && x.total_actoutput > 0);

            var downtimeQuery = ssc_db.VO_DOWNTIME
                .Where(x => x.date_ >= startDate && x.date_ <= finishDate
                       && x.mold != null && x.op_kpk != null && x.op_kpk != "00220" && x.downtime_code != "PD9" && x.downtime_code != "Z02");

            var notrunningQuery = ssc_db.VO_DOWNTIME
                .Where(x => x.date_ >= startDate && x.date_ <= finishDate
                       && x.mold != null && x.op_kpk != null && x.op_kpk != "00220" && (x.downtime_code == "PD9" || x.downtime_code == "Z02"));

            var topOutputQuery = ssc_db.VO_MAIN_CALC
                .Where(x => x.date_ >= startDate && x.date_ <= finishDate
                       && x.mold != null && x.op_kpk != null && x.op_kpk != "00220" && x.total_time > 0);


            // Mendapatkan data utama dari database
            var outputPart = ssc_db.VO_MAIN_CALC.Where(x => x.date_ >= startDate && x.date_ <= finishDate && x.mold != null && x.op_kpk != null && x.op_kpk != "00220").
                OrderBy(x => x.date_).ThenBy(x => x.no_machine).ThenBy(x => x.shift_).ToList();

            // Mendapatkan data utama dari database
            var outputOEE_ = outputQuery
                .GroupBy(x => new { x.we_, x.date_, x.no_machine, x.shift_ })
                .Select(g => new
                {
                    we_ = g.Key.we_,
                    date_ = g.Key.date_,
                    no_machine = g.Key.no_machine,
                    shift_ = g.Key.shift_,
                    total_time = g.Sum(x => x.total_time),
                    total_good_output = g.Sum(x => x.total_good_output),
                    total_actoutput = g.Sum(x => x.total_actoutput),
                    total_output_pcs = g.Sum(x => x.total_output_pcs),
                    pn_stdoutput = g.Sum(x => x.pn_stdoutput),
                    pn_operatingtime = g.Sum(x => x.pn_operatingtime),
                    totalRJ = g.Sum(x => x.totalRJ),
                    totalDT = g.Sum(x => x.totalDT),
                    totalNotRun = g.Sum(x => x.totalNotRun)
                })
                .OrderBy(x => x.date_).ThenBy(x => x.shift_).ThenBy(x => x.no_machine)
                .ToList(); // Eksekusi query dan materialize hasilnya

            var outputOEE = outputOEE_.Select(x => new VO_MAIN_CALC
            {
                we_ = x.we_,
                date_ = x.date_,
                no_machine = x.no_machine,
                shift_ = x.shift_,
                total_time = x.total_time,
                total_good_output = x.total_good_output,
                total_actoutput = x.total_actoutput,
                total_output_pcs = x.total_output_pcs,
                pn_stdoutput = x.pn_stdoutput,
                pn_operatingtime = x.pn_operatingtime,
                totalRJ = x.totalRJ,
                totalDT = x.totalDT,
                totalNotRun = x.totalNotRun,
            }).ToList(); // Membuat objek VO_MAIN_CALC setelah query dijalankan

            // Mendapatkan data Reject dari database
            // var outputRejectx = ssc_db.VO_REJECT.Where(x => x.date_ >= startDate && x.date_ <= finishDate && x.mold != null && x.op_kpk != "00220" && x.total_actoutput != null && x.total_actoutput > 0).
            //    OrderBy(x => x.date_).ThenBy(x => x.shift_).ThenBy(x => x.no_machine).ThenBy(x => x.reject_code).ToList();

            // Mendapatkan data Reject dari database
            var outputReject = rejectQuery.
                 GroupBy(x => new { x.we_, x.date_, x.shift_, x.no_machine, x.partnumber, x.reject_code, x.rj_remarks })
                 .Select(g => new
                 {
                     we_ = g.Key.we_,
                     date_ = g.Key.date_,
                     shift_ = g.Key.shift_,
                     no_machine = g.Key.no_machine,
                     partnumber = g.Key.partnumber,
                     reject_code = g.Key.reject_code,
                     rj_remarks = g.Key.rj_remarks,
                     reject_qty = g.Sum(x => x.reject_qty)

                 })
                 .OrderBy(x => x.date_).ThenBy(x => x.shift_).ThenBy(x => x.no_machine).ThenBy(x => x.reject_code).ToList()
                 .Select(x => new VO_REJECT
                 {
                     we_ = x.we_,
                     date_ = x.date_,
                     shift_ = x.shift_,
                     no_machine = x.no_machine,
                     partnumber = x.partnumber,
                     reject_code = x.reject_code,
                     rj_remarks = x.rj_remarks,
                     reject_qty = x.reject_qty
                 }).ToList();

            // Mendapatkan data Downtime dari database
            // var outputDowntimeX = ssc_db.VO_DOWNTIME.Where(x => x.date_ >= startDate && x.date_ <= finishDate && x.mold != null && x.op_kpk != "00220" && (x.downtime_code != "PD9" && x.downtime_code != "Z02")).
            //    OrderBy(x => x.date_).ThenBy(x => x.shift_).ThenBy(x => x.no_machine).ThenBy(x => x.dt_start).ToList();

            var outputDowntime = downtimeQuery.
                GroupBy(x => new { x.we_, x.date_, x.shift_, x.no_machine, x.partnumber, x.dt_start, x.dt_finish, x.downtime_code, x.dt_remarks, x.dt_type, x.op_kpk, x.op_name, x.downtime_notes })
                 .Select(g => new
                 {
                     we_ = g.Key.we_,
                     date_ = g.Key.date_,
                     shift_ = g.Key.shift_,
                     no_machine = g.Key.no_machine,
                     partnumber = g.Key.partnumber,
                     dt_start = g.Key.dt_start,
                     dt_finish = g.Key.dt_finish,
                     downtime_code = g.Key.downtime_code,
                     dt_remarks = g.Key.dt_remarks,
                     dt_type = g.Key.dt_type,
                     op_kpk = g.Key.op_kpk,
                     op_name = g.Key.op_name,
                     downtime_notes = g.Key.downtime_notes,
                     duration_minutes = g.Sum(x => x.duration_minutes)
                 })
                 .OrderBy(x => x.date_).ThenBy(x => x.shift_).ThenBy(x => x.no_machine).ThenBy(x => x.dt_start).ToList()
                 .Select(x => new VO_DOWNTIME
                 {
                     we_ = x.we_,
                     date_ = x.date_,
                     shift_ = x.shift_,
                     no_machine = x.no_machine,
                     partnumber = x.partnumber,
                     dt_start = x.dt_start,
                     dt_finish = x.dt_finish,
                     downtime_code = x.downtime_code,
                     dt_remarks = x.dt_remarks,
                     dt_type = x.dt_type,
                     duration_minutes = x.duration_minutes,
                     op_kpk = x.op_kpk,
                     op_name = x.op_name,
                     downtime_notes = x.downtime_notes
                 }).ToList();

            // Mendapatkan data Not Running dari database
            var outputNotRunning = notrunningQuery.
                 GroupBy(x => new { x.we_, x.date_, x.shift_, x.no_machine, x.partnumber, x.dt_start, x.dt_finish, x.downtime_code, x.dt_remarks, x.dt_type, x.op_kpk, x.op_name })
                 .Select(g => new
                 {
                     we_ = g.Key.we_,
                     date_ = g.Key.date_,
                     shift_ = g.Key.shift_,
                     no_machine = g.Key.no_machine,
                     partnumber = g.Key.partnumber,
                     dt_start = g.Key.dt_start,
                     dt_finish = g.Key.dt_finish,
                     downtime_code = g.Key.downtime_code,
                     dt_remarks = g.Key.dt_remarks,
                     dt_type = g.Key.dt_type,
                     op_kpk = g.Key.op_kpk,
                     op_name = g.Key.op_name,
                     duration_minutes = g.Sum(x => x.duration_minutes)
                 })
                 .OrderBy(x => x.date_).ThenBy(x => x.shift_).ThenBy(x => x.no_machine).ThenBy(x => x.dt_start).ToList()
                 .Select(x => new VO_DOWNTIME
                 {
                     we_ = x.we_,
                     date_ = x.date_,
                     shift_ = x.shift_,
                     no_machine = x.no_machine,
                     partnumber = x.partnumber,
                     dt_start = x.dt_start,
                     dt_finish = x.dt_finish,
                     downtime_code = x.downtime_code,
                     dt_remarks = x.dt_remarks,
                     dt_type = x.dt_type,
                     duration_minutes = x.duration_minutes,
                     op_kpk = x.op_kpk,
                     op_name = x.op_name
                 }).ToList();

            // List Doentime
            var listDowntime = ssc_db.pim_downtime.OrderBy(x => x.dt_code).ThenBy(x => x.dt_type).ToList();

            // >>> LIST OF MACHINE <<< //
            var listMachine = ssc_db.VO_MAIN_CALC.Where(x => x.mold != null && x.op_kpk != null && x.op_name != null).OrderBy(x => x.no_machine).Select(x => x.no_machine).Distinct().ToList();


            // Membuat file Excel menggunakan library EPPlus
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var package = new ExcelPackage())
            {
                int row = 2;
                int count = 1;

                // ===========================OUTPUT OEE=================================== //
                // Membuat worksheet_oee baru
                var worksheet_oee = package.Workbook.Worksheets.Add("OEE");

                // Menambahkan header kolom
                worksheet_oee.Cells[1, 1].Value = "ID";
                worksheet_oee.Cells[1, 2].Value = "Date";
                worksheet_oee.Cells[1, 3].Value = "WE";
                worksheet_oee.Cells[1, 4].Value = "Machine";
                worksheet_oee.Cells[1, 5].Value = "Shift";

                worksheet_oee.Cells[1, 6].Value = "Total Time (TT)_hrs";
                worksheet_oee.Cells[1, 7].Value = "Break_mins";
                worksheet_oee.Cells[1, 8].Value = "Briefing_mins";

                worksheet_oee.Cells[1, 9].Value = "Available Time_hrs";
                worksheet_oee.Cells[1, 10].Value = "Total Breakdown Time_hrs";
                worksheet_oee.Cells[1, 11].Value = "Operating Time_hrs";
                worksheet_oee.Cells[1, 12].Value = "Cycle Time_sec";

                worksheet_oee.Cells[1, 13].Value = "Standard Output";
                worksheet_oee.Cells[1, 14].Value = "Actual Output";
                worksheet_oee.Cells[1, 15].Value = "Reject";
                worksheet_oee.Cells[1, 16].Value = "Good Output";
                worksheet_oee.Cells[1, 17].Value = "Var";

                worksheet_oee.Cells[1, 18].Value = "Availability";
                worksheet_oee.Cells[1, 19].Value = "Performance";
                worksheet_oee.Cells[1, 20].Value = "Quality";
                worksheet_oee.Cells[1, 21].Value = "OEE";
                worksheet_oee.Cells[1, 22].Value = "Column1";

                foreach (var ssc in outputOEE)
                {
                    int available_time = (ssc.total_time != null ? (int)ssc.total_time : 0) - (ssc.totalNotRun != null ? (int)ssc.totalNotRun : 0);
                    int operating_time = available_time - (ssc.totalDT != null ? (int)ssc.totalDT : 0);
                    decimal availability = 0; decimal performance = 0; decimal quality = 0;

                    // AVAILABILITY
                    if (ssc.total_time.HasValue && ssc.total_time != 0 && ssc.pn_operatingtime.HasValue && ssc.pn_operatingtime != 0 && ssc.totalNotRun.HasValue && ssc.totalNotRun != 0)
                    {
                        availability = Math.Round((((decimal)ssc.pn_operatingtime) / ((decimal)ssc.total_time - (decimal)ssc.totalNotRun)), 2);
                    }
                    else if (ssc.total_time.HasValue && ssc.total_time != 0 && ssc.pn_operatingtime.HasValue && ssc.pn_operatingtime != 0)
                    {
                        availability = Math.Round((((decimal)ssc.pn_operatingtime) / ((decimal)ssc.total_time - 0)), 2);
                    }
                    else
                    {
                        availability = 0;
                    }

                    // PERFORMANCE
                    if (ssc.pn_stdoutput.HasValue && ssc.pn_stdoutput != 0 && ssc.total_output_pcs.HasValue && ssc.total_output_pcs != 0)
                    {
                        performance = Math.Round(((decimal)ssc.total_output_pcs / (decimal)ssc.pn_stdoutput), 2);
                    }
                    else
                    {
                        performance = 0;
                    }

                    // QUALITY
                    if (ssc.total_good_output.HasValue && ssc.total_good_output != 0 && ssc.total_output_pcs.HasValue && ssc.total_output_pcs != 0)
                    {
                        quality = Math.Round((((decimal)ssc.total_good_output) / (decimal)ssc.total_output_pcs), 2);
                    }
                    else
                    {
                        quality = 0;
                    }


                    // Jika performance lebih dari 1, maka setting == MAX 1  
                    if (performance >= 1.2m)
                    {
                        performance = 1.2m;
                    }


                    // OEE
                    decimal OEE_SCORE = 0;
                    if (availability == 0 || performance == 0 || quality == 0)
                    {
                        OEE_SCORE = 0;
                    }
                    else
                    {
                        OEE_SCORE = Math.Round((availability * performance * quality) / 1000000, 2);
                    }

                    // hitung total break per main_id
                    //int break_mins = 0;
                    //var break_minsGroups = ssc_db.VO_DOWNTIME
                    //    .Where(x => x.main_id_dt == ssc.main_id && x.downtime_code == "Z02")
                    //    .GroupBy(x => x.duration_minutes);

                    ////.GetValueOrDefault(), memastikan nilai duration_minutes dari setiap dalam grup akan diambil jika tidak null,
                    //// dan jika nilai null, maka akan diambil nilai defaultnya (0).
                    //foreach (var group in break_minsGroups)
                    //{
                    //    break_mins += group.Sum(x => x.duration_minutes.GetValueOrDefault());
                    //}


                    //worksheet_oee.Cells[row, 1].Value = count;
                    worksheet_oee.Cells[row, 1].Value = ssc.date_ + ssc.no_machine + ssc.shift_;
                    worksheet_oee.Cells[row, 2].Value = ssc.date_;
                    worksheet_oee.Cells[row, 3].Value = ssc.we_;
                    worksheet_oee.Cells[row, 4].Value = ssc.no_machine;
                    worksheet_oee.Cells[row, 5].Value = ssc.shift_;

                    worksheet_oee.Cells[row, 6].Value = ssc.total_time;
                    worksheet_oee.Cells[row, 7].Value = '0'; // break_mins
                    worksheet_oee.Cells[row, 8].Value = '0'; // briefing

                    worksheet_oee.Cells[row, 9].Value = available_time;
                    worksheet_oee.Cells[row, 10].Value = ssc.totalDT;
                    worksheet_oee.Cells[row, 11].Value = ssc.pn_operatingtime;
                    worksheet_oee.Cells[row, 12].Value = ssc.pn_ct;

                    worksheet_oee.Cells[row, 13].Value = ssc.pn_stdoutput;
                    worksheet_oee.Cells[row, 14].Value = ssc.total_actoutput;
                    worksheet_oee.Cells[row, 15].Value = ssc.totalRJ; // reject
                    worksheet_oee.Cells[row, 16].Value = ssc.total_good_output; // good output

                    // VAR
                    decimal variance_ = 0;
                    decimal actualOutputpcs = ssc.total_output_pcs ?? 0;
                    decimal standardOutput = ssc.pn_stdoutput ?? 0;

                    if (ssc.total_output_pcs >= ssc.pn_stdoutput)
                    {
                        variance_ = Math.Round(actualOutputpcs - standardOutput, 0);
                        worksheet_oee.Cells[row, 17].Value = variance_;
                    }
                    else
                    {
                        variance_ = Math.Round(standardOutput - actualOutputpcs, 0);
                        worksheet_oee.Cells[row, 17].Value = "-" + variance_;
                    }

                    // dikali 1 hasilnya satuan decimal dikali 100 hasilnya puluhan
                    worksheet_oee.Cells[row, 18].Value = Math.Round((availability) * 1, 2);
                    worksheet_oee.Cells[row, 19].Value = Math.Round((performance) * 1, 2);
                    worksheet_oee.Cells[row, 20].Value = Math.Round((quality) * 1, 2);
                    worksheet_oee.Cells[row, 21].Value = Math.Round((availability * performance * quality) * 1, 2);


                    row++;
                    count++;
                }



                // ===========================OUTPUT DOWNTIME=================================== //
                int row_dt = 2;
                int count_dt = 1;

                // Menambah worksheet baru
                var worksheet_dt = package.Workbook.Worksheets.Add("DT");
                // Menambahkan header kolom
                worksheet_dt.Cells[1, 1].Value = "No";
                worksheet_dt.Cells[1, 2].Value = "Date";
                worksheet_dt.Cells[1, 3].Value = "Machine";
                worksheet_dt.Cells[1, 4].Value = "Shift";
                worksheet_dt.Cells[1, 5].Value = "Part Number";
                worksheet_dt.Cells[1, 6].Value = "Nama Operator";
                worksheet_dt.Cells[1, 7].Value = "Start";
                worksheet_dt.Cells[1, 8].Value = "Finish";
                worksheet_dt.Cells[1, 9].Value = "Durasi";
                worksheet_dt.Cells[1, 10].Value = "DT Code";
                worksheet_dt.Cells[1, 11].Value = "Remarks";
                worksheet_dt.Cells[1, 12].Value = "Type";

                foreach (var ssc in outputDowntime)
                {
                    worksheet_dt.Cells[row_dt, 1].Value = count_dt;
                    worksheet_dt.Cells[row_dt, 2].Value = ssc.date_;
                    worksheet_dt.Cells[row_dt, 3].Value = ssc.no_machine;
                    worksheet_dt.Cells[row_dt, 4].Value = ssc.shift_;
                    worksheet_dt.Cells[row_dt, 5].Value = ssc.partnumber;
                    worksheet_dt.Cells[row_dt, 6].Value = ssc.op_name;
                    worksheet_dt.Cells[row_dt, 7].Value = ssc.dt_start;
                    worksheet_dt.Cells[row_dt, 8].Value = ssc.dt_finish;
                    worksheet_dt.Cells[row_dt, 9].Value = ssc.duration_minutes;
                    worksheet_dt.Cells[row_dt, 10].Value = ssc.downtime_code;
                    worksheet_dt.Cells[row_dt, 11].Value = ssc.dt_remarks;
                    worksheet_dt.Cells[row_dt, 12].Value = ssc.dt_type;

                    row_dt++;
                    count_dt++;
                }


                // ===========================OUTPUT LIST DOWNTIME=================================== //
                int row_l = 2;
                int count_l = 1;

                // Menambah worksheet baru
                var worksheet_l = package.Workbook.Worksheets.Add("List");
                // Menambahkan header kolom
                worksheet_l.Cells[1, 1].Value = "Kode";
                worksheet_l.Cells[1, 2].Value = "Downtime Remarks";
                worksheet_l.Cells[1, 3].Value = "Downtime Type";

                foreach (var dt in listDowntime)
                {
                    worksheet_l.Cells[row_l, 1].Value = count_dt;
                    worksheet_l.Cells[row_l, 2].Value = dt.dt_remarks;
                    worksheet_l.Cells[row_l, 3].Value = dt.dt_type;

                    row_l++;
                    count_l++;
                }

                // ===========================MACHINE LIST MACHINE=================================== //
                int row_mc = 2;
                int count_mc = 1;

                // Menambah worksheet baru
                var worksheet_mc = package.Workbook.Worksheets.Add("Machine List");
                // Menambahkan header kolom
                worksheet_mc.Cells[1, 1].Value = "Machine";

                foreach (var mc in listMachine)
                {
                    worksheet_mc.Cells[row_mc, 1].Value = mc;

                    row_mc++;
                    count_dt++;
                }


                // Mengatur lebar kolom otomatis
                worksheet_oee.Cells.AutoFitColumns();
                worksheet_dt.Cells.AutoFitColumns();
                worksheet_l.Cells.AutoFitColumns();

                // Menyimpan file Excel ke MemoryStream
                var memoryStream = new MemoryStream();
                package.SaveAs(memoryStream);
                memoryStream.Position = 0;

                // Send file Excel sebagai unduhan ke user
                var excelName = "PTMI_InjectionMolding_OEE.xlsx";
                var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                return File(memoryStream, contentType, excelName);
            }
            // Redirect to innerDataMC with the updated user_id
            // return RedirectToAction("inputScrap");
        }

        // DOWNLOAD DATA OUTPUT PART - EXPORT TO EXCEL
        [HttpGet]
        public ActionResult exportOutputOEE(DateTime? dateStart = null, DateTime? dateEnd = null)
        {
            //========================================================//

            DateTime now = DateTime.Now;
            DateTime startDate = dateStart ?? now.Date;
            DateTime finishDate = dateEnd ?? now.Date;

            ViewBag.DateNow = now;
            ViewBag.startDate = startDate;
            ViewBag.finishDate = finishDate;

            // Declare Variable Data Output
            var outputQuery = ssc_db.VO_MAIN_CALC
                .Where(x => x.date_ >= startDate && x.date_ <= finishDate
                       && x.mold != null && x.op_kpk != null && x.op_kpk != "00220" && x.total_time > 0);

            var rejectQuery = ssc_db.VO_REJECT
                .Where(x => x.date_ >= startDate && x.date_ <= finishDate
                       && x.mold != null && x.op_kpk != null && x.op_kpk != "00220" && x.total_actoutput != null && x.total_actoutput > 0);

            var downtimeQuery = ssc_db.VO_DOWNTIME
                .Where(x => x.date_ >= startDate && x.date_ <= finishDate
                       && x.mold != null && x.op_kpk != null && x.op_kpk != "00220" && x.downtime_code != "PD9" && x.downtime_code != "Z02");

            var notrunningQuery = ssc_db.VO_DOWNTIME
                .Where(x => x.date_ >= startDate && x.date_ <= finishDate
                       && x.mold != null && x.op_kpk != null && x.op_kpk != "00220" && (x.downtime_code == "PD9" || x.downtime_code == "Z02"));

            var topOutputQuery = ssc_db.VO_MAIN_CALC
                .Where(x => x.date_ >= startDate && x.date_ <= finishDate
                       && x.mold != null && x.op_kpk != null && x.op_kpk != "00220" && x.total_time > 0);


            // Mendapatkan data utama dari database
            var outputPart = ssc_db.VO_MAIN_CALC.Where(x => x.date_ >= startDate && x.date_ <= finishDate && x.mold != null && x.op_kpk != null && x.op_kpk != "00220").
                OrderBy(x => x.date_).ThenBy(x => x.no_machine).ThenBy(x => x.shift_).ToList();

            // Mendapatkan data utama dari database
            var outputOEE_ = outputQuery
                .GroupBy(x => new { x.we_, x.date_, x.no_machine, x.shift_ })
                .Select(g => new
                {
                    we_ = g.Key.we_,
                    date_ = g.Key.date_,
                    no_machine = g.Key.no_machine,
                    shift_ = g.Key.shift_,
                    total_time = g.Sum(x => x.total_time),
                    total_good_output = g.Sum(x => x.total_good_output),
                    total_actoutput = g.Sum(x => x.total_actoutput),
                    total_output_pcs = g.Sum(x => x.total_output_pcs),
                    pn_stdoutput = g.Sum(x => x.pn_stdoutput),
                    pn_operatingtime = g.Sum(x => x.pn_operatingtime),
                    totalRJ = g.Sum(x => x.totalRJ),
                    totalDT = g.Sum(x => x.totalDT),
                    totalNotRun = g.Sum(x => x.totalNotRun)
                })
                .OrderBy(x => x.date_).ThenBy(x => x.shift_).ThenBy(x => x.no_machine)
                .ToList(); // Eksekusi query dan materialize hasilnya

            var outputOEE = outputOEE_.Select(x => new VO_MAIN_CALC
            {
                we_ = x.we_,
                date_ = x.date_,
                no_machine = x.no_machine,
                shift_ = x.shift_,
                total_time = x.total_time,
                total_good_output = x.total_good_output,
                total_actoutput = x.total_actoutput,
                total_output_pcs = x.total_output_pcs,
                pn_stdoutput = x.pn_stdoutput,
                pn_operatingtime = x.pn_operatingtime,
                totalRJ = x.totalRJ,
                totalDT = x.totalDT,
                totalNotRun = x.totalNotRun,
            }).ToList(); // Membuat objek VO_MAIN_CALC setelah query dijalankan

            // Mendapatkan data Reject dari database
            // var outputRejectx = ssc_db.VO_REJECT.Where(x => x.date_ >= startDate && x.date_ <= finishDate && x.mold != null && x.op_kpk != "00220" && x.total_actoutput != null && x.total_actoutput > 0).
            //    OrderBy(x => x.date_).ThenBy(x => x.shift_).ThenBy(x => x.no_machine).ThenBy(x => x.reject_code).ToList();

            // Mendapatkan data Reject dari database
            var outputReject = rejectQuery.
                 GroupBy(x => new { x.we_, x.date_, x.shift_, x.no_machine, x.partnumber, x.reject_code, x.rj_remarks })
                 .Select(g => new
                 {
                     we_ = g.Key.we_,
                     date_ = g.Key.date_,
                     shift_ = g.Key.shift_,
                     no_machine = g.Key.no_machine,
                     partnumber = g.Key.partnumber,
                     reject_code = g.Key.reject_code,
                     rj_remarks = g.Key.rj_remarks,
                     reject_qty = g.Sum(x => x.reject_qty)

                 })
                 .OrderBy(x => x.date_).ThenBy(x => x.shift_).ThenBy(x => x.no_machine).ThenBy(x => x.reject_code).ToList()
                 .Select(x => new VO_REJECT
                 {
                     we_ = x.we_,
                     date_ = x.date_,
                     shift_ = x.shift_,
                     no_machine = x.no_machine,
                     partnumber = x.partnumber,
                     reject_code = x.reject_code,
                     rj_remarks = x.rj_remarks,
                     reject_qty = x.reject_qty
                 }).ToList();

            // Mendapatkan data Downtime dari database
            // var outputDowntimeX = ssc_db.VO_DOWNTIME.Where(x => x.date_ >= startDate && x.date_ <= finishDate && x.mold != null && x.op_kpk != "00220" && (x.downtime_code != "PD9" && x.downtime_code != "Z02")).
            //    OrderBy(x => x.date_).ThenBy(x => x.shift_).ThenBy(x => x.no_machine).ThenBy(x => x.dt_start).ToList();

            var outputDowntime = downtimeQuery.
                GroupBy(x => new { x.we_, x.date_, x.shift_, x.no_machine, x.partnumber, x.dt_start, x.dt_finish, x.downtime_code, x.dt_remarks, x.dt_type, x.op_kpk, x.op_name, x.downtime_notes })
                 .Select(g => new
                 {
                     we_ = g.Key.we_,
                     date_ = g.Key.date_,
                     shift_ = g.Key.shift_,
                     no_machine = g.Key.no_machine,
                     partnumber = g.Key.partnumber,
                     dt_start = g.Key.dt_start,
                     dt_finish = g.Key.dt_finish,
                     downtime_code = g.Key.downtime_code,
                     dt_remarks = g.Key.dt_remarks,
                     dt_type = g.Key.dt_type,
                     op_kpk = g.Key.op_kpk,
                     op_name = g.Key.op_name,
                     downtime_notes = g.Key.downtime_notes,
                     duration_minutes = g.Sum(x => x.duration_minutes)
                 })
                 .OrderBy(x => x.date_).ThenBy(x => x.shift_).ThenBy(x => x.no_machine).ThenBy(x => x.dt_start).ToList()
                 .Select(x => new VO_DOWNTIME
                 {
                     we_ = x.we_,
                     date_ = x.date_,
                     shift_ = x.shift_,
                     no_machine = x.no_machine,
                     partnumber = x.partnumber,
                     dt_start = x.dt_start,
                     dt_finish = x.dt_finish,
                     downtime_code = x.downtime_code,
                     dt_remarks = x.dt_remarks,
                     dt_type = x.dt_type,
                     duration_minutes = x.duration_minutes,
                     op_kpk = x.op_kpk,
                     op_name = x.op_name,
                     downtime_notes = x.downtime_notes
                 }).ToList();

            // Mendapatkan data Not Running dari database
            var outputNotRunning = notrunningQuery.
                 GroupBy(x => new { x.we_, x.date_, x.shift_, x.no_machine, x.partnumber, x.dt_start, x.dt_finish, x.downtime_code, x.dt_remarks, x.dt_type, x.op_kpk, x.op_name })
                 .Select(g => new
                 {
                     we_ = g.Key.we_,
                     date_ = g.Key.date_,
                     shift_ = g.Key.shift_,
                     no_machine = g.Key.no_machine,
                     partnumber = g.Key.partnumber,
                     dt_start = g.Key.dt_start,
                     dt_finish = g.Key.dt_finish,
                     downtime_code = g.Key.downtime_code,
                     dt_remarks = g.Key.dt_remarks,
                     dt_type = g.Key.dt_type,
                     op_kpk = g.Key.op_kpk,
                     op_name = g.Key.op_name,
                     duration_minutes = g.Sum(x => x.duration_minutes)
                 })
                 .OrderBy(x => x.date_).ThenBy(x => x.shift_).ThenBy(x => x.no_machine).ThenBy(x => x.dt_start).ToList()
                 .Select(x => new VO_DOWNTIME
                 {
                     we_ = x.we_,
                     date_ = x.date_,
                     shift_ = x.shift_,
                     no_machine = x.no_machine,
                     partnumber = x.partnumber,
                     dt_start = x.dt_start,
                     dt_finish = x.dt_finish,
                     downtime_code = x.downtime_code,
                     dt_remarks = x.dt_remarks,
                     dt_type = x.dt_type,
                     duration_minutes = x.duration_minutes,
                     op_kpk = x.op_kpk,
                     op_name = x.op_name
                 }).ToList();


            // Membuat file Excel menggunakan library EPPlus
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var package = new ExcelPackage())
            {
                int row = 2;
                int count = 1;

                // ===========================OUTPUT OEE=================================== //
                // Membuat worksheet_oee baru
                var worksheet_oee = package.Workbook.Worksheets.Add("OEE");

                // Menambahkan header kolom
                worksheet_oee.Cells[1, 1].Value = "Date";
                worksheet_oee.Cells[1, 2].Value = "WE";
                worksheet_oee.Cells[1, 3].Value = "Machine";
                worksheet_oee.Cells[1, 4].Value = "Shift";
                worksheet_oee.Cells[1, 5].Value = "Total Time";
                worksheet_oee.Cells[1, 6].Value = "Not Running";
                worksheet_oee.Cells[1, 7].Value = "Available Time";
                worksheet_oee.Cells[1, 8].Value = "Total Downtime";
                worksheet_oee.Cells[1, 9].Value = "Operating Time";
                worksheet_oee.Cells[1, 10].Value = "Standard Output";
                worksheet_oee.Cells[1, 11].Value = "Output Pcs";
                worksheet_oee.Cells[1, 12].Value = "Var";
                worksheet_oee.Cells[1, 13].Value = "Availability";
                worksheet_oee.Cells[1, 14].Value = "Performance";
                worksheet_oee.Cells[1, 15].Value = "Quality";
                worksheet_oee.Cells[1, 16].Value = "OEE";
                // ------------------------------------- TAMPILKAN RATA-RATA TOTAL OEE ------------------------------------- //

                foreach (var ssc in outputOEE)
                {
                    int available_time = (ssc.total_time != null ? (int)ssc.total_time : 0) - (ssc.totalNotRun != null ? (int)ssc.totalNotRun : 0);
                    int operating_time = available_time - (ssc.totalDT != null ? (int)ssc.totalDT : 0);
                    decimal availability = 0; decimal performance = 0; decimal quality = 0;

                    // AVAILABILITY
                    if (ssc.total_time.HasValue && ssc.total_time != 0 && ssc.pn_operatingtime.HasValue && ssc.pn_operatingtime != 0 && ssc.totalNotRun.HasValue && ssc.totalNotRun != 0)
                    {
                        availability = Math.Round((((decimal)ssc.pn_operatingtime) / ((decimal)ssc.total_time - (decimal)ssc.totalNotRun)), 2);
                    }
                    else if (ssc.total_time.HasValue && ssc.total_time != 0 && ssc.pn_operatingtime.HasValue && ssc.pn_operatingtime != 0)
                    {
                        availability = Math.Round((((decimal)ssc.pn_operatingtime) / (decimal)ssc.total_time), 2);
                    }
                    else
                    {
                        availability = 0;
                    }

                    // PERFORMANCE
                    if (ssc.pn_stdoutput.HasValue && ssc.pn_stdoutput != 0 && ssc.total_output_pcs.HasValue && ssc.total_output_pcs != 0)
                    {
                        performance = Math.Round(((decimal)ssc.total_output_pcs / (decimal)ssc.pn_stdoutput), 2);
                    }
                    else
                    {
                        performance = 0;
                    }

                    // QUALITY
                    if (ssc.total_good_output.HasValue && ssc.total_good_output != 0 && ssc.total_output_pcs.HasValue && ssc.total_output_pcs != 0)
                    {
                        quality = Math.Round((((decimal)ssc.total_good_output) / (decimal)ssc.total_output_pcs), 2);
                    }
                    else
                    {
                        quality = 0;
                    }



                    // Jika performance lebih dari 1, maka setting == MAX 1 -

                    if (performance >= 1.2m)
                    {
                        performance = 1.2m;
                    }


                    // OEE
                    decimal OEE_SCORE = 0;
                    if (availability == 0 || performance == 0 || quality == 0)
                    {
                        OEE_SCORE = 0;
                    }
                    else
                    {
                        OEE_SCORE = Math.Round((availability * performance * quality) / 1000000, 2);
                    }

                    worksheet_oee.Cells[row, 1].Value = count;
                    worksheet_oee.Cells[row, 1].Value = ssc.date_;
                    worksheet_oee.Cells[row, 2].Value = ssc.we_;
                    worksheet_oee.Cells[row, 3].Value = ssc.no_machine;
                    worksheet_oee.Cells[row, 4].Value = ssc.shift_;
                    worksheet_oee.Cells[row, 5].Value = ssc.total_time;
                    worksheet_oee.Cells[row, 6].Value = ssc.totalNotRun;
                    worksheet_oee.Cells[row, 7].Value = available_time;
                    worksheet_oee.Cells[row, 8].Value = ssc.totalDT;
                    worksheet_oee.Cells[row, 9].Value = ssc.pn_operatingtime;
                    worksheet_oee.Cells[row, 10].Value = ssc.pn_stdoutput;
                    worksheet_oee.Cells[row, 11].Value = ssc.total_output_pcs;

                    // dikali 1 hasilnya satuan decimal dikali 100 hasilnya puluhan
                    worksheet_oee.Cells[row, 13].Value = Math.Round((availability) * 1, 2);
                    worksheet_oee.Cells[row, 14].Value = Math.Round((performance) * 1, 2);
                    worksheet_oee.Cells[row, 15].Value = Math.Round((quality) * 1, 2);
                    worksheet_oee.Cells[row, 16].Value = Math.Round((availability * performance * quality) * 1, 2);

                    decimal variance_ = 0;
                    decimal actualOutputpcs = ssc.total_output_pcs ?? 0;
                    decimal standardOutput = ssc.pn_stdoutput ?? 0;

                    if (ssc.total_output_pcs >= ssc.pn_stdoutput)
                    {
                        variance_ = Math.Round(actualOutputpcs - standardOutput, 0);
                        worksheet_oee.Cells[row, 12].Value = variance_;
                    }
                    else
                    {
                        variance_ = Math.Round(standardOutput - actualOutputpcs, 0);
                        worksheet_oee.Cells[row, 12].Value = "-" + variance_;
                    }

                    row++;
                    count++;
                }


                // ===========================OUTPUT PART=================================== //
                int row_o = 2;
                int count_o = 1;

                // Menambah worksheet baru
                var worksheet_opart = package.Workbook.Worksheets.Add("Output Part");

                // Menambahkan header kolom
                worksheet_opart.Cells[1, 1].Value = "Date";
                worksheet_opart.Cells[1, 2].Value = "Machine";
                worksheet_opart.Cells[1, 3].Value = "Shift";
                worksheet_opart.Cells[1, 4].Value = "Part Number";
                worksheet_opart.Cells[1, 5].Value = "KPK";
                worksheet_opart.Cells[1, 6].Value = "Operator";
                worksheet_opart.Cells[1, 7].Value = "Total Time";
                worksheet_opart.Cells[1, 8].Value = "Not Running";
                worksheet_opart.Cells[1, 9].Value = "Total Downtime";
                worksheet_opart.Cells[1, 10].Value = "CAV";
                worksheet_opart.Cells[1, 11].Value = "SET";
                worksheet_opart.Cells[1, 12].Value = "Pengali";
                worksheet_opart.Cells[1, 13].Value = "Actual Output";
                worksheet_opart.Cells[1, 14].Value = "Actual Output (pcs)";
                worksheet_opart.Cells[1, 15].Value = "Total Reject";
                worksheet_opart.Cells[1, 16].Value = "Good Output";
                worksheet_opart.Cells[1, 17].Value = "Available Time";
                worksheet_opart.Cells[1, 18].Value = "Operating Time";
                worksheet_opart.Cells[1, 19].Value = "Standard Output";
                worksheet_opart.Cells[1, 20].Value = "Variance";

                foreach (var ssc in outputPart)
                {
                    int available_time = (ssc.total_time != null ? (int)ssc.total_time : 0) - (ssc.totalNotRun != null ? (int)ssc.totalNotRun : 0);
                    int operating_time = available_time - (ssc.totalDT != null ? (int)ssc.totalDT : 0);

                    worksheet_opart.Cells[row_o, 1].Value = count_o;
                    worksheet_opart.Cells[row_o, 1].Value = ssc.date_;
                    worksheet_opart.Cells[row_o, 2].Value = ssc.no_machine;
                    worksheet_opart.Cells[row_o, 3].Value = ssc.shift_;
                    worksheet_opart.Cells[row_o, 4].Value = ssc.partnumber;
                    worksheet_opart.Cells[row_o, 5].Value = ssc.op_kpk;
                    worksheet_opart.Cells[row_o, 6].Value = ssc.op_name;
                    worksheet_opart.Cells[row_o, 7].Value = ssc.total_time;
                    worksheet_opart.Cells[row_o, 8].Value = ssc.totalNotRun;
                    worksheet_opart.Cells[row_o, 9].Value = ssc.totalDT;
                    worksheet_opart.Cells[row_o, 10].Value = ssc.pn_cav;
                    worksheet_opart.Cells[row_o, 11].Value = ssc.pn_set;
                    worksheet_opart.Cells[row_o, 12].Value = ssc.pengalioutput;
                    worksheet_opart.Cells[row_o, 13].Value = ssc.total_actoutput;
                    worksheet_opart.Cells[row_o, 14].Value = ssc.total_output_pcs;
                    worksheet_opart.Cells[row_o, 15].Value = ssc.totalRJ;
                    worksheet_opart.Cells[row_o, 16].Value = ssc.total_good_output;
                    worksheet_opart.Cells[row_o, 17].Value = available_time;
                    worksheet_opart.Cells[row_o, 18].Value = operating_time;
                    worksheet_opart.Cells[row_o, 19].Value = ssc.pn_stdoutput;
                    //worksheet_opart.Cells[row_o, 20].Value = ssc.pn_mchrs;

                    decimal variance_ = 0;
                    decimal actualOutputpcs = ssc.total_output_pcs ?? 0;
                    decimal standardOutput = ssc.pn_stdoutput ?? 0;

                    if (ssc.total_output_pcs >= ssc.pn_stdoutput)
                    {
                        variance_ = Math.Round(actualOutputpcs - standardOutput, 0);
                        worksheet_opart.Cells[row_o, 20].Value = variance_;
                    }
                    else
                    {
                        variance_ = Math.Round(standardOutput - actualOutputpcs, 0);
                        worksheet_opart.Cells[row_o, 20].Value = "-" + variance_;
                    }

                    row_o++;
                    count_o++;
                }



                // ===========================OUTPUT REJECT=================================== //
                int row_rj = 2;
                int count_rj = 1;

                // Menambah worksheet baru
                var worksheet_rj = package.Workbook.Worksheets.Add("REJECT");
                // Menambahkan header kolom
                worksheet_rj.Cells[1, 1].Value = "WE";
                worksheet_rj.Cells[1, 2].Value = "Date";
                worksheet_rj.Cells[1, 3].Value = "Machine";
                worksheet_rj.Cells[1, 4].Value = "Shift";
                worksheet_rj.Cells[1, 5].Value = "Part Number";
                worksheet_rj.Cells[1, 6].Value = "Quantity";
                worksheet_rj.Cells[1, 7].Value = "Code";
                worksheet_rj.Cells[1, 8].Value = "Details";
                worksheet_rj.Cells[1, 9].Value = "Other Remarks";

                foreach (var ssc in outputReject)
                {
                    worksheet_rj.Cells[row_rj, 1].Value = count_rj;
                    worksheet_rj.Cells[row_rj, 1].Value = ssc.we_;
                    worksheet_rj.Cells[row_rj, 2].Value = ssc.date_;
                    worksheet_rj.Cells[row_rj, 3].Value = ssc.no_machine;
                    worksheet_rj.Cells[row_rj, 4].Value = ssc.shift_;
                    worksheet_rj.Cells[row_rj, 5].Value = ssc.partnumber;
                    worksheet_rj.Cells[row_rj, 6].Value = ssc.reject_qty;
                    worksheet_rj.Cells[row_rj, 7].Value = ssc.reject_code;
                    worksheet_rj.Cells[row_rj, 8].Value = ssc.rj_remarks;
                    worksheet_rj.Cells[row_rj, 9].Value = ssc.reject_notes;

                    row_rj++;
                    count_rj++;
                }


                // ===========================OUTPUT DOWNTIME=================================== //
                int row_dt = 2;
                int count_dt = 1;

                // Menambah worksheet baru
                var worksheet_dt = package.Workbook.Worksheets.Add("DT");
                // Menambahkan header kolom
                worksheet_dt.Cells[1, 1].Value = "WE";
                worksheet_dt.Cells[1, 2].Value = "Date";
                worksheet_dt.Cells[1, 3].Value = "Machine";
                worksheet_dt.Cells[1, 4].Value = "Shift";
                worksheet_dt.Cells[1, 5].Value = "Part Number";
                worksheet_dt.Cells[1, 6].Value = "KPK";
                worksheet_dt.Cells[1, 7].Value = "Operator";
                worksheet_dt.Cells[1, 8].Value = "Start";
                worksheet_dt.Cells[1, 9].Value = "Finish";
                worksheet_dt.Cells[1, 10].Value = "Durations (min)";
                worksheet_dt.Cells[1, 11].Value = "DT Code";
                worksheet_dt.Cells[1, 12].Value = "Remarks";
                worksheet_dt.Cells[1, 13].Value = "Type";
                worksheet_dt.Cells[1, 14].Value = "Additional Description";

                foreach (var ssc in outputDowntime)
                {
                    worksheet_dt.Cells[row_dt, 1].Value = count_dt;
                    worksheet_dt.Cells[row_dt, 1].Value = ssc.we_;
                    worksheet_dt.Cells[row_dt, 2].Value = ssc.date_;
                    worksheet_dt.Cells[row_dt, 3].Value = ssc.no_machine;
                    worksheet_dt.Cells[row_dt, 4].Value = ssc.shift_;
                    worksheet_dt.Cells[row_dt, 5].Value = ssc.partnumber;
                    worksheet_dt.Cells[row_dt, 6].Value = ssc.op_kpk;
                    worksheet_dt.Cells[row_dt, 7].Value = ssc.op_name;
                    worksheet_dt.Cells[row_dt, 8].Value = ssc.dt_start;
                    worksheet_dt.Cells[row_dt, 9].Value = ssc.dt_finish;
                    worksheet_dt.Cells[row_dt, 10].Value = ssc.duration_minutes;
                    worksheet_dt.Cells[row_dt, 11].Value = ssc.downtime_code;
                    worksheet_dt.Cells[row_dt, 12].Value = ssc.dt_remarks;
                    worksheet_dt.Cells[row_dt, 13].Value = ssc.dt_type;
                    worksheet_dt.Cells[row_dt, 14].Value = ssc.downtime_notes;

                    row_dt++;
                    count_dt++;
                }


                // ===========================OUTPUT NOT RUNNING=================================== //

                int row_dtnr = 2;
                int count_dtnr = 1;
                // Menambah worksheet baru
                var worksheet_dtnr = package.Workbook.Worksheets.Add("DT NR");

                // Menambahkan header kolom
                worksheet_dtnr.Cells[1, 1].Value = "WE";
                worksheet_dtnr.Cells[1, 2].Value = "Date";
                worksheet_dtnr.Cells[1, 3].Value = "Machine";
                worksheet_dtnr.Cells[1, 4].Value = "Shift";
                worksheet_dtnr.Cells[1, 5].Value = "Part Number";
                worksheet_dtnr.Cells[1, 6].Value = "KPK";
                worksheet_dtnr.Cells[1, 7].Value = "Operator";
                worksheet_dtnr.Cells[1, 8].Value = "Start";
                worksheet_dtnr.Cells[1, 9].Value = "Finish";
                worksheet_dtnr.Cells[1, 10].Value = "Durations (min)";
                worksheet_dtnr.Cells[1, 11].Value = "DT Code";
                worksheet_dtnr.Cells[1, 12].Value = "Remarks";
                worksheet_dtnr.Cells[1, 13].Value = "Type";
                worksheet_dtnr.Cells[1, 14].Value = "Additional Description";

                foreach (var ssc in outputNotRunning)
                {
                    worksheet_dtnr.Cells[row_dtnr, 1].Value = count_dtnr;
                    worksheet_dtnr.Cells[row_dtnr, 1].Value = ssc.we_;
                    worksheet_dtnr.Cells[row_dtnr, 2].Value = ssc.date_;
                    worksheet_dtnr.Cells[row_dtnr, 3].Value = ssc.no_machine;
                    worksheet_dtnr.Cells[row_dtnr, 4].Value = ssc.shift_;
                    worksheet_dtnr.Cells[row_dtnr, 5].Value = ssc.partnumber;
                    worksheet_dtnr.Cells[row_dtnr, 6].Value = ssc.op_kpk;
                    worksheet_dtnr.Cells[row_dtnr, 7].Value = ssc.op_name;
                    worksheet_dtnr.Cells[row_dtnr, 8].Value = ssc.dt_start;
                    worksheet_dtnr.Cells[row_dtnr, 9].Value = ssc.dt_finish;
                    worksheet_dtnr.Cells[row_dtnr, 10].Value = ssc.duration_minutes;
                    worksheet_dtnr.Cells[row_dtnr, 11].Value = ssc.downtime_code;
                    worksheet_dtnr.Cells[row_dtnr, 12].Value = ssc.dt_remarks;
                    worksheet_dtnr.Cells[row_dtnr, 13].Value = ssc.dt_type;
                    worksheet_dtnr.Cells[row_dtnr, 14].Value = ssc.downtime_notes;

                    row_dtnr++;
                    count_dtnr++;
                }

                // Mengatur lebar kolom otomatis
                worksheet_oee.Cells.AutoFitColumns();
                worksheet_opart.Cells.AutoFitColumns();
                worksheet_rj.Cells.AutoFitColumns();
                worksheet_dt.Cells.AutoFitColumns();
                worksheet_dtnr.Cells.AutoFitColumns();

                // Menyimpan file Excel ke MemoryStream
                var memoryStream = new MemoryStream();
                package.SaveAs(memoryStream);
                memoryStream.Position = 0;

                // Send file Excel sebagai unduhan ke user
                var excelName = "SSC_OuputOEE.xlsx";
                var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                return File(memoryStream, contentType, excelName);
            }
            // Redirect to innerDataMC with the updated user_id
            // return RedirectToAction("inputScrap");
        }

        // DOWNLOAD DATA OUTPUT PART - EXPORT TO EXCEL
        [HttpGet]
        public ActionResult exportOutputPart(DateTime? dateStart = null, DateTime? dateFinish = null)
        {
            //========================================================//

            DateTime now = DateTime.Now;
            DateTime startDate = dateStart ?? now.Date;
            DateTime finishDate = dateFinish ?? now.Date;

            ViewBag.DateNow = now;
            ViewBag.startDate = startDate;
            ViewBag.finishDate = finishDate;

            //========================================================//

            // Mendapatkan data dari database
            var outputPart = ssc_db.VO_MAIN_CALC.Where(x => x.date_ >= startDate && x.date_ <= finishDate && x.mold != null && x.op_kpk != null && x.op_kpk != "00220").
                OrderBy(x => x.date_).ThenBy(x => x.no_machine).ThenBy(x => x.shift_).ToList();

            // Membuat file Excel menggunakan library EPPlus
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var package = new ExcelPackage())
            {
                // Membuat worksheet baru
                var worksheet = package.Workbook.Worksheets.Add("Output Part");

                // Menambahkan header kolom
                worksheet.Cells[1, 1].Value = "Date";
                worksheet.Cells[1, 2].Value = "Machine";
                worksheet.Cells[1, 3].Value = "Shift";
                worksheet.Cells[1, 4].Value = "Part Number";
                worksheet.Cells[1, 5].Value = "KPK";
                worksheet.Cells[1, 6].Value = "Operator";
                worksheet.Cells[1, 7].Value = "Total Time";
                worksheet.Cells[1, 8].Value = "Not Running";
                worksheet.Cells[1, 9].Value = "Total Downtime";
                worksheet.Cells[1, 10].Value = "CAV";
                worksheet.Cells[1, 11].Value = "SET";
                worksheet.Cells[1, 12].Value = "Pengali";
                worksheet.Cells[1, 13].Value = "Actual Output";
                worksheet.Cells[1, 14].Value = "Actual Output (pcs)";
                worksheet.Cells[1, 15].Value = "Total Reject";
                worksheet.Cells[1, 16].Value = "Good Output";
                worksheet.Cells[1, 17].Value = "Available Time";
                worksheet.Cells[1, 18].Value = "Operating Time";
                worksheet.Cells[1, 19].Value = "Standard Output";
                //worksheet.Cells[1, 20].Value = "Mchrs";
                worksheet.Cells[1, 20].Value = "Variance";

                // Menambahkan data scrap ke worksheet
                // continue from line 3
                //int row = 2;
                int row = 2;
                int count = 1;
                foreach (var ssc in outputPart)
                {
                    int available_time = (ssc.total_time != null ? (int)ssc.total_time : 0) - (ssc.totalNotRun != null ? (int)ssc.totalNotRun : 0);
                    int operating_time = available_time - (ssc.totalDT != null ? (int)ssc.totalDT : 0);

                    worksheet.Cells[row, 1].Value = count;
                    worksheet.Cells[row, 1].Value = ssc.date_;
                    worksheet.Cells[row, 2].Value = ssc.no_machine;
                    worksheet.Cells[row, 3].Value = ssc.shift_;
                    worksheet.Cells[row, 4].Value = ssc.partnumber;
                    worksheet.Cells[row, 5].Value = ssc.op_kpk;
                    worksheet.Cells[row, 6].Value = ssc.op_name;
                    worksheet.Cells[row, 7].Value = ssc.total_time;
                    worksheet.Cells[row, 8].Value = ssc.totalNotRun;
                    worksheet.Cells[row, 9].Value = ssc.totalDT;
                    worksheet.Cells[row, 10].Value = ssc.pn_cav;
                    worksheet.Cells[row, 11].Value = ssc.pn_set;
                    worksheet.Cells[row, 12].Value = ssc.pengalioutput;
                    worksheet.Cells[row, 13].Value = ssc.total_actoutput;
                    worksheet.Cells[row, 14].Value = ssc.total_output_pcs;
                    worksheet.Cells[row, 15].Value = ssc.totalRJ;
                    worksheet.Cells[row, 16].Value = ssc.total_good_output;
                    worksheet.Cells[row, 17].Value = available_time;
                    worksheet.Cells[row, 18].Value = operating_time;
                    worksheet.Cells[row, 19].Value = ssc.pn_stdoutput;
                    //worksheet.Cells[row, 20].Value = ssc.pn_mchrs;

                    decimal variance_ = 0;
                    decimal actualOutputpcs = ssc.total_output_pcs ?? 0;
                    decimal standardOutput = ssc.pn_stdoutput ?? 0;

                    if (ssc.total_output_pcs >= ssc.pn_stdoutput)
                    {
                        variance_ = Math.Round(actualOutputpcs - standardOutput, 0);
                        worksheet.Cells[row, 20].Value = variance_;
                    }
                    else
                    {
                        variance_ = Math.Round(standardOutput - actualOutputpcs, 0);
                        worksheet.Cells[row, 20].Value = "-" + variance_;
                    }

                    row++;
                    count++;
                }

                // Mengatur lebar kolom otomatis
                worksheet.Cells.AutoFitColumns();

                // Menyimpan file Excel ke MemoryStream
                var memoryStream = new MemoryStream();
                package.SaveAs(memoryStream);
                memoryStream.Position = 0;

                // Send file Excel sebagai unduhan ke user
                var excelName = "SSC_OuputPart.xlsx";
                var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                return File(memoryStream, contentType, excelName);
            }
            // Redirect to innerDataMC with the updated user_id
            // return RedirectToAction("inputScrap");
        }

        private string GetLeaderArea(int? areaCode)
        {
            if (areaCode.ToString().Contains("1"))
            {
                return "1F-A,B,C,D,M3";
            }
            else if (areaCode.ToString().Contains("2"))
            {
                return "1F-E,F,G,H";
            }
            else if (areaCode.ToString().Contains("3"))
            {
                return "1F-J,K,L";
            }
            else if (areaCode.ToString().Contains("4"))
            {
                return "1F-M,N,P";
            }
            else if (areaCode.ToString().Contains("5"))
            {
                return "GF-A,B,C,D,E,F";
            }
            else if (areaCode.ToString().Contains("6"))
            {
                return "GF-G,H,J,K,L,M";
            }
            else
            {
                // Lakukan sesuatu jika areaCode tidak cocok dengan kondisi di atas
                return "not valid";
            }
        }

        // DOWNLOAD DATA REJECT - EXPORT TO EXCEL
        [HttpGet]
        public ActionResult exportOutput_PP(DateTime? dateStart = null, DateTime? dateFinish = null)
        {
            //========================================================//

            DateTime now = DateTime.Now;
            DateTime startDate = dateStart ?? now.Date;
            DateTime finishDate = dateFinish ?? now.Date;

            ViewBag.DateNow = now;
            ViewBag.startDate = startDate;
            ViewBag.finishDate = finishDate;

            //========================================================//

            // read data - relationship table reject PP with user 
            var outputReject_PP = ssc_db.VO_REJECT_PP.Where(x => x.date_ >= startDate && x.date_ <= finishDate && x.part_pp != null && x.op_kpk != null && x.op_kpk != "00220" && x.total_actoutput != null && x.total_actoutput > 0).
                OrderBy(x => x.date_).ThenBy(x => x.shift_).ThenBy(x => x.no_machine).ThenBy(x => x.reject_code).ToList();

            // Membuat file Excel menggunakan library EPPlus
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var package = new ExcelPackage())
            {
                // Membuat worksheet baru
                var worksheet = package.Workbook.Worksheets.Add("Output Reject PP");

                // Menambahkan header kolom
                worksheet.Cells[1, 1].Value = "No";
                worksheet.Cells[1, 2].Value = "Date";
                worksheet.Cells[1, 3].Value = "Shift";
                worksheet.Cells[1, 4].Value = "Leader Area";
                worksheet.Cells[1, 5].Value = "Machine";
                worksheet.Cells[1, 6].Value = "Mold";
                worksheet.Cells[1, 7].Value = "Part Number";
                worksheet.Cells[1, 8].Value = "Description";
                worksheet.Cells[1, 9].Value = "CT";
                worksheet.Cells[1, 10].Value = "Output/Jam";
                worksheet.Cells[1, 11].Value = "Plan";
                worksheet.Cells[1, 12].Value = "Output";
                worksheet.Cells[1, 13].Value = "Output (Pcs)";
                worksheet.Cells[1, 14].Value = "Reject Qty";
                worksheet.Cells[1, 15].Value = "Reject Remark";

                // Menambahkan data scrap ke worksheet
                // continue from line 3
                //int row = 2;
                int row = 2;
                int count = 1;
                foreach (var ssc in outputReject_PP)
                {
                    worksheet.Cells[row, 1].Value = count;
                    worksheet.Cells[row, 2].Value = ssc.date_;
                    worksheet.Cells[row, 3].Value = ssc.shift_;
                    worksheet.Cells[row, 4].Value = GetLeaderArea(ssc.mc_area);
                    worksheet.Cells[row, 5].Value = ssc.no_machine;
                    worksheet.Cells[row, 6].Value = ssc.mold;
                    worksheet.Cells[row, 7].Value = ssc.partnumber;
                    worksheet.Cells[row, 8].Value = ssc.DESC;
                    // hitung standard output jika memiliki nilai cav dan ct
                    if (ssc.pn_ct != null && ssc.pn_cav != null)
                    {
                        worksheet.Cells[row, 9].Value = ssc.pn_ct;
                        worksheet.Cells[row, 10].Value = (3600 / ssc.pn_ct) * ssc.pn_cav; //(3600 / item.pn_ct) * item.pn_cav;

                    } else
                    {
                        worksheet.Cells[row, 9].Value = 0;
                        worksheet.Cells[row, 10].Value = 0;
                    }
                    worksheet.Cells[row, 11].Value = ssc.sch;
                    worksheet.Cells[row, 12].Value = ssc.total_actoutput;
                    worksheet.Cells[row, 13].Value = ssc.total_output_pcs;
                    worksheet.Cells[row, 14].Value = ssc.reject_qty;
                    worksheet.Cells[row, 15].Value = ssc.rj_remarks;

                    row++;
                    count++;
                }

                // Mengatur lebar kolom otomatis
                worksheet.Cells.AutoFitColumns();

                // Menyimpan file Excel ke MemoryStream
                var memoryStream = new MemoryStream();
                package.SaveAs(memoryStream);
                memoryStream.Position = 0;

                // Send file Excel sebagai unduhan ke user
                var excelName = "SSC_Ouput_PP.xlsx";
                var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                return File(memoryStream, contentType, excelName);
            }
            // Redirect to innerDataMC with the updated user_id
            // return RedirectToAction("inputScrap");
        }
        
        // DOWNLOAD DATA REJECT - EXPORT TO EXCEL
        [HttpGet]
        public ActionResult exportReject(DateTime? dateStart = null, DateTime? dateFinish = null)
        {
            //========================================================//

            DateTime now = DateTime.Now;
            DateTime startDate = dateStart ?? now.Date;
            DateTime finishDate = dateFinish ?? now.Date;

            ViewBag.DateNow = now;
            ViewBag.startDate = startDate;
            ViewBag.finishDate = finishDate;

            //========================================================//

            // Mendapatkan data dari database
            var outputReject = ssc_db.VO_REJECT.Where(x => x.date_ >= startDate && x.date_ <= finishDate && x.mold != null && x.op_kpk != "00220" && x.total_actoutput != null && x.total_actoutput > 0).
                OrderBy(x => x.date_).ThenBy(x => x.shift_).ThenBy(x => x.no_machine).ThenBy(x => x.reject_code).ToList();

            // Membuat file Excel menggunakan library EPPlus
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var package = new ExcelPackage())
            {
                // Membuat worksheet baru
                var worksheet = package.Workbook.Worksheets.Add("Output Reject");

                // Menambahkan header kolom
                worksheet.Cells[1, 1].Value = "WE";
                worksheet.Cells[1, 2].Value = "Date";
                worksheet.Cells[1, 3].Value = "Machine";
                worksheet.Cells[1, 4].Value = "Shift";
                worksheet.Cells[1, 5].Value = "Part Number";
                worksheet.Cells[1, 6].Value = "Quantity";
                worksheet.Cells[1, 7].Value = "Code";
                worksheet.Cells[1, 8].Value = "Details";
                worksheet.Cells[1, 9].Value = "Other Remarks";

                // Menambahkan data scrap ke worksheet
                // continue from line 3
                //int row = 2;
                int row = 2;
                int count = 1;
                foreach (var ssc in outputReject)
                {
                    worksheet.Cells[row, 1].Value = count;
                    worksheet.Cells[row, 1].Value = ssc.we_;
                    worksheet.Cells[row, 2].Value = ssc.date_;
                    worksheet.Cells[row, 3].Value = ssc.no_machine;
                    worksheet.Cells[row, 4].Value = ssc.shift_;
                    worksheet.Cells[row, 5].Value = ssc.partnumber;
                    worksheet.Cells[row, 6].Value = ssc.reject_qty;
                    worksheet.Cells[row, 7].Value = ssc.reject_code;
                    worksheet.Cells[row, 8].Value = ssc.rj_remarks;
                    worksheet.Cells[row, 9].Value = ssc.reject_notes;

                    row++;
                    count++;
                }

                // Mengatur lebar kolom otomatis
                worksheet.Cells.AutoFitColumns();

                // Menyimpan file Excel ke MemoryStream
                var memoryStream = new MemoryStream();
                package.SaveAs(memoryStream);
                memoryStream.Position = 0;

                // Send file Excel sebagai unduhan ke user
                var excelName = "SSC_OuputReject.xlsx";
                var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                return File(memoryStream, contentType, excelName);
            }
            // Redirect to innerDataMC with the updated user_id
            // return RedirectToAction("inputScrap");
        }



        // DOWNLOAD DATA DOWNTIME - EXPORT TO EXCEL
        [HttpGet]
        public ActionResult exportDowntime(DateTime? dateStart = null, DateTime? dateFinish = null)
        {
            //========================================================//

            DateTime now = DateTime.Now;
            DateTime startDate = dateStart ?? now.Date;
            DateTime finishDate = dateFinish ?? now.Date;

            ViewBag.DateNow = now;
            ViewBag.startDate = startDate;
            ViewBag.finishDate = finishDate;

            //========================================================//

            // Mendapatkan data dari database
            var outputDowntime = ssc_db.VO_DOWNTIME.Where(x => x.date_ >= startDate && x.date_ <= finishDate && x.mold != null && x.op_kpk != "00220" && (x.downtime_code != "PD09" || x.downtime_code != "Z02")).
                OrderBy(x => x.date_).ThenBy(x => x.shift_).ThenBy(x => x.no_machine).ThenBy(x => x.dt_start).ToList();

            // Membuat file Excel menggunakan library EPPlus
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var package = new ExcelPackage())
            {
                // Membuat worksheet baru
                var worksheet = package.Workbook.Worksheets.Add("Output Downtime");

                // Menambahkan header kolom
                worksheet.Cells[1, 1].Value = "WE";
                worksheet.Cells[1, 2].Value = "Date";
                worksheet.Cells[1, 3].Value = "Machine";
                worksheet.Cells[1, 4].Value = "Shift";
                worksheet.Cells[1, 5].Value = "Part Number";
                worksheet.Cells[1, 6].Value = "KPK";
                worksheet.Cells[1, 7].Value = "Operator";
                worksheet.Cells[1, 8].Value = "Start";
                worksheet.Cells[1, 9].Value = "Finish";
                worksheet.Cells[1, 10].Value = "Durations";
                worksheet.Cells[1, 11].Value = "Code";
                worksheet.Cells[1, 12].Value = "Remarks";
                worksheet.Cells[1, 13].Value = "Type";
                worksheet.Cells[1, 14].Value = "Notes";


                // Menambahkan data scrap ke worksheet
                // continue from line 3
                //int row = 2;
                int row = 2;
                int count = 1;
                foreach (var ssc in outputDowntime)
                {
                    worksheet.Cells[row, 1].Value = count;
                    worksheet.Cells[row, 1].Value = ssc.we_;
                    worksheet.Cells[row, 2].Value = ssc.date_;
                    worksheet.Cells[row, 3].Value = ssc.no_machine;
                    worksheet.Cells[row, 4].Value = ssc.shift_;
                    worksheet.Cells[row, 5].Value = ssc.partnumber;
                    worksheet.Cells[row, 6].Value = ssc.op_kpk;
                    worksheet.Cells[row, 7].Value = ssc.op_name;
                    worksheet.Cells[row, 8].Value = ssc.dt_start;
                    worksheet.Cells[row, 9].Value = ssc.dt_finish;
                    worksheet.Cells[row, 10].Value = ssc.duration_minutes;
                    worksheet.Cells[row, 11].Value = ssc.hour_counter;
                    worksheet.Cells[row, 12].Value = ssc.dt_remarks;
                    worksheet.Cells[row, 13].Value = ssc.dt_type;
                    worksheet.Cells[row, 14].Value = ssc.downtime_notes;

                    row++;
                    count++;
                }

                // Mengatur lebar kolom otomatis
                worksheet.Cells.AutoFitColumns();

                // Menyimpan file Excel ke MemoryStream
                var memoryStream = new MemoryStream();
                package.SaveAs(memoryStream);
                memoryStream.Position = 0;

                // Send file Excel sebagai unduhan ke user
                var excelName = "SSC_OuputDowntime.xlsx";
                var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                return File(memoryStream, contentType, excelName);
            }
            // Redirect to innerDataMC with the updated user_id
            // return RedirectToAction("inputScrap");
        }

        // DOWNLOAD DATA NOT RUNNING - EXPORT TO EXCEL
        [HttpGet]
        public ActionResult exportNotRunning(DateTime? dateStart = null, DateTime? dateFinish = null)
        {
            //========================================================//

            DateTime now = DateTime.Now;
            DateTime startDate = dateStart ?? now.Date;
            DateTime finishDate = dateFinish ?? now.Date;

            ViewBag.DateNow = now;
            ViewBag.startDate = startDate;
            ViewBag.finishDate = finishDate;

            //========================================================//

            // Mendapatkan data dari database
            var outputNotRunning = ssc_db.VO_DOWNTIME.Where(x => x.date_ >= startDate && x.date_ <= finishDate && x.mold != null && x.op_kpk != "00220" && (x.downtime_code == "PD09" || x.downtime_code == "Z02")).
                OrderBy(x => x.date_).ThenBy(x => x.shift_).ThenBy(x => x.no_machine).ThenBy(x => x.dt_start).ToList();

            // Membuat file Excel menggunakan library EPPlus
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var package = new ExcelPackage())
            {
                // Membuat worksheet baru
                var worksheet = package.Workbook.Worksheets.Add("Output Downtime");

                // Menambahkan header kolom
                worksheet.Cells[1, 1].Value = "WE";
                worksheet.Cells[1, 2].Value = "Date";
                worksheet.Cells[1, 3].Value = "Machine";
                worksheet.Cells[1, 4].Value = "Shift";
                worksheet.Cells[1, 5].Value = "Part Number";
                worksheet.Cells[1, 6].Value = "KPK";
                worksheet.Cells[1, 7].Value = "Operator";
                worksheet.Cells[1, 8].Value = "Start";
                worksheet.Cells[1, 9].Value = "Finish";
                worksheet.Cells[1, 10].Value = "Durations";
                worksheet.Cells[1, 11].Value = "Code";
                worksheet.Cells[1, 12].Value = "Remarks";
                worksheet.Cells[1, 13].Value = "Type";
                worksheet.Cells[1, 14].Value = "Notes";


                // Menambahkan data scrap ke worksheet
                // continue from line 3
                //int row = 2;
                int row = 2;
                int count = 1;
                foreach (var ssc in outputNotRunning)
                {
                    worksheet.Cells[row, 1].Value = count;
                    worksheet.Cells[row, 1].Value = ssc.we_;
                    worksheet.Cells[row, 2].Value = ssc.date_;
                    worksheet.Cells[row, 3].Value = ssc.no_machine;
                    worksheet.Cells[row, 4].Value = ssc.shift_;
                    worksheet.Cells[row, 5].Value = ssc.partnumber;
                    worksheet.Cells[row, 6].Value = ssc.op_kpk;
                    worksheet.Cells[row, 7].Value = ssc.op_name;
                    worksheet.Cells[row, 8].Value = ssc.dt_start;
                    worksheet.Cells[row, 9].Value = ssc.dt_finish;
                    worksheet.Cells[row, 10].Value = ssc.duration_minutes;
                    worksheet.Cells[row, 11].Value = ssc.downtime_code;
                    worksheet.Cells[row, 12].Value = ssc.dt_remarks;
                    worksheet.Cells[row, 13].Value = ssc.dt_type;
                    worksheet.Cells[row, 14].Value = ssc.downtime_notes;

                    row++;
                    count++;
                }

                // Mengatur lebar kolom otomatis
                worksheet.Cells.AutoFitColumns();

                // Menyimpan file Excel ke MemoryStream
                var memoryStream = new MemoryStream();
                package.SaveAs(memoryStream);
                memoryStream.Position = 0;

                // Send file Excel sebagai unduhan ke user
                var excelName = "SSC_OuputNotRunning.xlsx";
                var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                return File(memoryStream, contentType, excelName);
            }
            // Redirect to innerDataMC with the updated user_id
            // return RedirectToAction("inputScrap");
        }

    }
}