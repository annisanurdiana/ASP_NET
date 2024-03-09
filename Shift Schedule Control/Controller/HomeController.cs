// semua logic ini sdh dipertimbangkan dengan matang, jika logic mau dirubah maka harus pertimbangkan matang-matang
// perhitungan ini saling terhubung satu sama lain, jika merubahnya maka akan mempengarui yang lainnya
// backup sebelum melakukan improvement pada code program maupun logicnya

using OEE_SSC.Models;
using OfficeOpenXml;
using OfficeOpenXml.Drawing.Slicer.Style;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Windows;
using System.Windows.Media.Media3D;
using System.Xml.Linq;
using static OEE_SSC.Models.PIM_SSC_MAIN;
using static System.Data.Entity.Infrastructure.Design.Executor;

namespace OEE_SSC.Controllers
{
    public class HomeController : Controller
    {
        // START - ENTITIES DATA MODEL //
        //MMSEntities ssc_db = new MMSEntities(); // PIM database here 
        masbroEntitiesPIM ssc_db = new masbroEntitiesPIM(); // PIM database here 

        MDSEntities pn_db = new MDSEntities(); // PN Information dari MDS db



        public ActionResult Indexx()
        {
            return View();
        }

        public ActionResult mainPage()
        {
            return View();
        }

        public ActionResult specificDataMC_test()
        {
            return View();
        }
        public ActionResult innerDataMC_test()
        {
            return View();
        }
        public ActionResult generalDataMC_test()
        {
            return View();
        }
        public ActionResult error404()
        {
            return View();
        }

        // Method untuk melakukan pembaruan pn_operatingtime
        private void UpdatePnOperatingTime()
        {
            using (masbroEntitiesPIM sql_query_update = new masbroEntitiesPIM()) // Gantilah dengan instance DbContext Anda
            {
                sql_query_update.Database.ExecuteSqlCommand(@"
                UPDATE dbo.PIM_SSC_CALCULATION
                SET pn_operatingtime = 
                    CASE
                        WHEN total_time IS NOT NULL AND totalDT IS NOT NULL AND totalNotRun IS NOT NULL
                            THEN total_time - totalDT - totalNotRun
                        WHEN total_time IS NOT NULL AND totalDT IS NOT NULL AND totalNotRun IS NULL
                            THEN total_time - totalDT
                        WHEN total_time IS NOT NULL AND totalNotRun IS NOT NULL AND totalDT IS NULL
                            THEN total_time - totalNotRun
                        WHEN total_time IS NOT NULL AND totalDT IS NULL AND totalNotRun IS NULL
                            THEN total_time
                        ELSE NULL
                    END
                FROM dbo.PIM_SSC_CALCULATION
                JOIN dbo.PIM_SSC_MAIN ON dbo.PIM_SSC_CALCULATION.main_id_tc = dbo.PIM_SSC_MAIN.main_id
                WHERE dbo.PIM_SSC_MAIN.op_kpk IS NOT NULL
            ");
            }
        }
        // Method untuk melakukan pembaruan pn_operatingtime
        private void UpdatePnStandardOutput()
        {
            using (masbroEntitiesPIM sql_query_update = new masbroEntitiesPIM()) // Gantilah dengan instance DbContext Anda
            {
                // 
                //data_calc.pn_stdoutput = (3600 / data_main.pn_ct) * data_main.pn_cav * (data_calc.pn_operatingtime / 60);

                sql_query_update.Database.ExecuteSqlCommand(@"
                    UPDATE dbo.PIM_SSC_CALCULATION
                    SET pn_stdoutput = CEILING(((3600 / main.pn_ct) * main.pn_cav * (calc.pn_operatingtime / 60.0)))
                    FROM dbo.PIM_SSC_CALCULATION calc
                    INNER JOIN dbo.PIM_SSC_MAIN main ON calc.main_id_tc = main.main_id
                    WHERE calc.pn_operatingtime IS NOT NULL
                      AND calc.main_id_tc IN (SELECT main_id FROM dbo.PIM_SSC_MAIN)
                ");
            }
        }


        // Delete Data MC
        public ActionResult deleteDataMC(int main_id)
        {
            // IMPORTANT: Data dihapus harus berurutan dan step by step, dan tidak bisa dilakukan sekaligus

            // cari data berdasarkan main_id
            var data_main = ssc_db.PIM_SSC_MAIN.Find(main_id);

            // jika data tidak ditemukan maka tampilkan error not found
            if (data_main == null)
            {
                return HttpNotFound();
            }
            // cari data-data downtime berdasarkan main_id
            var data_downtime = ssc_db.PIM_SSC_O_DOWNTIME.Where(x => x.main_id_dt == main_id).ToList();

            // jika data ditemukan maka remove semua downtime berdasarkan main_id
            if (data_downtime.Any())
            {
                ssc_db.PIM_SSC_O_DOWNTIME.RemoveRange(data_downtime);
                ssc_db.SaveChanges();
            }

            // cari data-data reject berdasarkan main_id
            var data_reject = ssc_db.PIM_SSC_O_REJECT.Where(x => x.main_id_rj == main_id).ToList();

            // jika data reject ditemukan maka remove semua reject berdasarkan main_id
            if (data_reject.Any())
            {
                ssc_db.PIM_SSC_O_REJECT.RemoveRange(data_reject);
                ssc_db.SaveChanges();
            }

            // cari data-data output ssc berdasarkan main_id
            var data_o = ssc_db.PIM_SSC_OUTPUT.Where(x => x.main_id_o == main_id).ToList();

            // jika data output ditemukan maka remove semua output berdasarkan main_id
            if (data_o.Any())
            {
                ssc_db.PIM_SSC_OUTPUT.RemoveRange(data_o);
                ssc_db.SaveChanges();
            }

            // cari data-data kalkulasi ssc berdasarkan main_id
            var data_calc = ssc_db.PIM_SSC_CALCULATION.Where(x => x.main_id_tc == main_id).ToList();

            // cari data-data kalkulasi ssc berdasarkan main_id
            if (data_calc.Any())
            {
                ssc_db.PIM_SSC_CALCULATION.RemoveRange(data_calc);
                ssc_db.SaveChanges();
            }

            // Deklarasi variable yang diperlukan
            var id_ = data_main.main_id;
            var date_ = data_main.date_;
            var shift_ = data_main.shift_;
            var n_machine_ = data_main.no_machine;
            var partnumber_ = data_main.partnumber;

            // Periksa column PART NUMBER, jika bernilai null maka delete row yang ada di database
            if (string.IsNullOrEmpty(partnumber_))
            {
                // Hapus baris jika semua data dalam baris telah dihapus
                ssc_db.PIM_SSC_MAIN.Remove(data_main);
                ssc_db.SaveChanges();


                TempData["Message"] = "Baris data dengan nomor machine = \"" + n_machine_ + "\" telah berhasil dihapus.";
            }
            // jika PART NUMBER tidak bernilai null, maka hanya hapus sebagian data ( KECUALI "id & no machine" tdk boleh di hapus) 
            else
            {

                // Hapus semua data yang dipilih:
                // data_main.partnumber = null;
                data_main.pengalioutput = null;
                data_main.pn_cav = null;
                data_main.pn_set = null;
                data_main.op_kpk = null;
                data_main.op_name = null;
                data_main.total_actoutput = null;

                ssc_db.Entry(data_main).State = EntityState.Modified;
                ssc_db.SaveChanges();

                TempData["Message"] = "informasi data dengan part number \"" + partnumber_ + "\" di nomor machine \"" + n_machine_ + "\" telah berhasil dihapus.";
            }

            // tetap berada di halaman utama SSC
            return RedirectToAction("firstPage", new { shiftEmp = shift_, dateFilter = date_ });
        }



        // ---------------------------------------------------------------------------------------------------------- //
        // Halaman ini untuk tampilan awal SSC dan list-list SSC tiap mesin
        public ActionResult firstPage(int shiftEmp = 0, DateTime? dateFilter = null)
        {
            // Jika dateFilter bernilai null, berarti nilai default yang digunakan adalah tanggal saat ini
            DateTime currentDate = dateFilter ?? DateTime.Now;

            // Jika waktu saat ini sudah melebihi jam 22.40, tambahkan 1 hari ke tanggal saat ini
            if (currentDate.Hour >= 22 && currentDate.Minute >= 40)
            {
                currentDate = currentDate.AddDays(1);
            }

            // Konversi tanggal ke format string "yyyy-MM-dd"
            string dateString = currentDate.ToString("yyyy-MM-dd");

            // simpan informasi date dan shift dalam bentuk session
            Session["myDate"] = dateFilter;
            Session["myShift"] = shiftEmp;

            //ViewBag.ErrorImport = TempData["ErrorImport"];
            //ViewBag.MessageImport = TempData["MessageImport"];

            // jika shiftEmp tidak diisi/null dan atau dateFilter == null, maka tetap di halaman landing page
            if (shiftEmp == 0 || (dateFilter == null))
            {
                return View();
            }
            // ---- jika value shift dan dateFilter diisi, maka jalankan kode dibawah ---- //
            else
            {
                // simpan nilai shiftEmp ke dalam ViewBag atau ViewData
                ViewBag.ShiftEmp = shiftEmp;

                // Berikut query untuk pengambilan data dari setiap tabel berdasarkan value 'shift' dan 'dateFilter' dari PIM
                var combinedData_ = new pimListData
                {
                    PIM_SSC_MAINs = ssc_db.PIM_SSC_MAIN.Where(x => x.shift_ == shiftEmp && x.date_ == dateFilter).OrderBy(x => x.no_machine).ToList(),
                };

                //// query untuk pengambilan data dari setiap tabel berdasarkan value 'shift' dan 'dateFilter' dari PA
                //var xxx = ssc_db.PIM_SSC_MAIN.Where(x => x.shift_ == shiftEmp && x.date_ == dateFilter).ToList();
                //if (xxx.Any())
                //{
                //    xxx = xxx.Select(x => new PIM_SSC_MAIN
                //    {
                //        shift_ = x.shift_,
                //        date_ = x.date_,
                //        downtime_code = int.Parse(x.downtime_code)
                //    }).ToList();
                //    ViewBag.ShiftEmp = shiftEmp;
                //    ViewBag.dateFilter = dateFilter;
                //    return View("generalDataMC", xxx);
                //}
                //else
                //{
                //    return RedirectToAction("insertSchedule", new { inputShift = shiftEmp, dateFilter = dateFilter });
                //}

                //var xxx = ssc_db.PIM_SSC_MAIN.Where(x => x.shift_ == shiftEmp && x.date_ == dateFilter).ToList();

                // jika combinedData ditemukan
                if (combinedData_.PIM_SSC_MAINs.Any())
                {
                    // untuk menampilkan informasi shift dan date yg pilih
                    ViewBag.ShiftEmp = shiftEmp;
                    ViewBag.dateFilter = dateFilter;

                    // tampilkan halaman view "generalDataMC" dengan membawa combinedData tersebut
                    return View("generalDataMC", combinedData_);

                }
                else
                {
                    // jika data belum ada, maka arahkan user ke halaman insertschedule, untuk menginport sch terlebih dulu
                    return RedirectToAction("insertSchedule", new { inputShift = shiftEmp, dateFilter = dateFilter });
                }

            }
        }


        // ------------------------------------------------------------------------- //

        public ActionResult filterByMachine(int shiftEmp = 0, string n_machine = "", DateTime? dateFilter = null)
        {
            // simpan session shift dan date sebagai variable
            int myShift = (int)System.Web.HttpContext.Current.Session["myShift"];
            DateTime myDate = (DateTime)System.Web.HttpContext.Current.Session["myDate"];

            // jika shiftEmp tidak ada value atau filter date tidak memiliki nilai, maka halaman yang ditampilkan tidak berubah
            if (shiftEmp == 0 || (dateFilter == null && string.IsNullOrEmpty(n_machine)))
            {
                return View();
            }
            //  ---- jika value n_machine dan value date tidak kosong, maka jalankan kode berikut... ---- //
            else if (!string.IsNullOrEmpty(n_machine) && dateFilter != null)
            {
                // simpan nilai shiftEmp ke dalam ViewBag atau ViewData
                ViewBag.ShiftEmp = shiftEmp;

                // simpan nilai n_machine ke dalam ViewBag 
                ViewBag.no_machine = n_machine;

                // simpan nilai dateFilter ke dalam ViewBag 
                ViewBag.dateFilter = myDate;


                // ================== //
                // If n_machine doesn't start with "1F" or "GF", take the substring of the first character,
                // If it starts with "m3", take the substring of the third character,
                // If it starts with "1F" or "GF", take the substring of the first four characters
                string searchMachine = !n_machine.StartsWith("1F") && !n_machine.StartsWith("GF") ?
                    (n_machine.StartsWith("m3") ? n_machine.Substring(2, 1) : n_machine.Substring(0, 1)) :
                    n_machine.Substring(0, 4);

                // Retrieve data from the database based on shift, date, and the substring depending on the above conditions,
                // order by no_machine, and convert it to a list
                var data_filter_machine = ssc_db.PIM_SSC_MAIN
                    .Where(x => x.shift_ == shiftEmp && x.date_ == dateFilter && x.no_machine.Contains(searchMachine))
                    .OrderBy(x => x.no_machine)
                    .ToList();

                // tambahkan beberapa query jika diperlukan dan simpan ke dalam variable combinedData
                var combinedData = new pimListData
                {
                    PIM_SSC_MAINs = data_filter_machine.ToList()
                };

                // If data is found, display the "generalDataMC" view using the retrieved data
                if (combinedData.PIM_SSC_MAINs.Any())
                {
                    return View("generalDataMC", combinedData);
                }
                // If data is not found, display the view "Error 404 Not Found"
                else
                {
                    return View("error404");
                }
            }
            // ---- jika no_machine kosong, kembali ke halaman awal, refresh halaman dengan membawa nilai shift dan date  ---- //
            else
            {
                // simpan nilai shiftEmp ke dalam ViewBag atau ViewData
                ViewBag.ShiftEmp = shiftEmp;

                // simpan nilai n_machine ke dalam ViewBag 
                ViewBag.no_machine = n_machine;

                // Contains() = mencari string dengan kata kunci tertentu
                // EndsWith() = mencari string yang diakhiri dengan kata kunci tertentu
                // StartsWith = mencari string yang diawali dengan kata kunci tertentu

                // ambil data dari database berdasarkan shift dan tanggal
                var data_filter_machine = ssc_db.PIM_SSC_MAIN
                   .Where(x => x.shift_ == shiftEmp && x.date_ == dateFilter)
                   .OrderBy(x => x.no_machine)
                   .ToList();

                // tambahkan beberapa query jika diperlukan dan simpan ke dalam variable combinedData
                var combinedData = new pimListData
                {
                    PIM_SSC_MAINs = data_filter_machine.ToList()
                };

                // jika combinedData available, tampilkan list ssc dengan menggunakan data yang telah difilter
                if (combinedData.PIM_SSC_MAINs.Any())
                {
                    return View("generalDataMC", combinedData);
                }
                // jika combinedData tidak ditemukan, tampilkan view error404 (bisa dikustom errornya)
                else
                {
                    return View("error404");
                }
            }
        }
        // ---------------------------------------------------------------------------------------------------------- //

        // Add New Data from user to database table pim_user_output
        [HttpGet]
        public ActionResult AddPartNumber()
        {

            return View();
        }
        [HttpPost]
        public ActionResult AddPartNumber(PIM_SSC_MAIN model)
        {
            // Retrieve user shift and date from session
            int myShift = (int)System.Web.HttpContext.Current.Session["myShift"];
            DateTime myDate = (DateTime)System.Web.HttpContext.Current.Session["myDate"];

            // Check if the specified KPK Operator is valid
            var user_op_kpk = ssc_db.pim_user_kpk.FirstOrDefault(m => m.op_kpk == model.op_kpk);
            if (user_op_kpk == null)
            {
                TempData["Error"] = "Failed: Please double-check the Operator's KPK";
                return RedirectToAction("firstPage", new { shiftEmp = myShift, dateFilter = myDate });
            }

            // Check if CAVITY is not greater than SET
            if (model.pn_cav < model.pn_set)
            {
                TempData["Error"] = "Failed: CAVITY value cannot be greater than SET value";
                return RedirectToAction("firstPage", new { shiftEmp = myShift, dateFilter = myDate });
            }

            // Check if SET is an odd number
            if (model.pn_set % 2 == 1)
            {
                TempData["Error"] = "Failed: SET value cannot be an odd number";
                return RedirectToAction("firstPage", new { shiftEmp = myShift, dateFilter = myDate });
            }

            // Calculate the week-ending date
            DateTime weekEnding = myDate.AddDays(6 - (int)myDate.DayOfWeek);
            string formattedWeekEnding = weekEnding.ToShortDateString();

            // Set values for the model
            model.we_ = formattedWeekEnding;
            model.op_name = user_op_kpk.op_name;
            model.shift_ = myShift;
            model.date_ = myDate;
            model.mc_area = GetMcArea(model.no_machine); // Private method

            // Add the model to the database and save changes
            ssc_db.PIM_SSC_MAIN.Add(model);
            ssc_db.SaveChanges();

            // Update SET value if not provided
            var first_input = ssc_db.PIM_SSC_MAIN.FirstOrDefault(m => m.main_id == model.main_id);
            if (model.pn_set == null || model.pn_set == 0)
            {
                model.pn_set = first_input.pn_cav;
                ssc_db.SaveChanges();
            }

            // Retrieve additional data from the pim_part_number_list table
            var partnumber_MDS = pn_db.MDS_PART_NUMBER_LIST.FirstOrDefault(m => m.PN == model.partnumber);
            if (partnumber_MDS != null)
            {
                model.pn_ct = partnumber_MDS.CT;
                model.mold = partnumber_MDS.MOLD;
                ssc_db.SaveChanges();
            }

            // Calculate and update pengalioutput value
            if ((model.pn_set != null && model.pn_set > 0) && (model.pn_cav != null && model.pn_cav > 0))
            {
                model.sch = model.sch;
                model.pengalioutput = (int)Math.Round((double)model.pn_cav / (double)model.pn_set, 2);
            }
            else
            {
                model.pengalioutput = 0;
            }
            ssc_db.SaveChanges();

            // Set messages for the view
            ViewBag.Message = model.main_id;
            ViewBag.Message = "Data Inserted Successfully!";

            // Redirect to innerDataMC page with the new data's ID
            return RedirectToAction("innerDataMC", new { main_id = model.main_id });


        }

        //======================================================================= //
        // OPEN TO READ SSC >> If it's still maintenance

        public ActionResult underMaintenance(int shiftEmp = 0, DateTime? dateFilter = null)
        {
            ViewBag.shift = shiftEmp;
            ViewBag.date = dateFilter;

            return View();
        }

        //======================================================================= //
        // Enter SSC List after locked / Dummy Login per machine

        public ActionResult loginEditingSSC(PIM_SSC_MAIN model, int main_id, string kpkEmpPIM, string pwEmpPIM)
        {
            // Retrieve user shift and date from session
            int myShift = (int)System.Web.HttpContext.Current.Session["myShift"];
            DateTime myDate = (DateTime)System.Web.HttpContext.Current.Session["myDate"];

            // Check if the specified KPK Operator is valid
            var mainid = ssc_db.PIM_SSC_MAIN.FirstOrDefault(m => m.main_id == model.main_id);

            // Jika data tidak ditemukan, berikan notifikasi error di halaman list SSC 
            if (mainid == null)
            {
                TempData["Error"] = "Failed: Data Not Found";
                return RedirectToAction("firstPage", new { shiftEmp = myShift, dateFilter = myDate });
            }
            else
            {
                // SSC Per Machine dikunci dan hanya user yang memiliki role admin dan super_admin yang dapat mengakses, super_admin adalah the app owner
                var registeredAdmin = ssc_db.pim_user_kpk.FirstOrDefault(m => m.op_kpk == kpkEmpPIM && (m.em_role == "admin" || m.em_role == "super_admin"));

                // jika yang mencoba masuk adalah selain role tersebut, berikan notifikasi error dan alasannya
                if (registeredAdmin == null)
                {
                    TempData["Error"] = "Failed: You are not registered as Admin, edit SSC not permitted";
                    return RedirectToAction("firstPage", new { shiftEmp = myShift, dateFilter = myDate });
                }
                else
                {
                    // temporary password untuk edit SSC
                    var pwEdit = "Testing123@";

                    // Check password, memakai variable yang berbeda, karena jika password dinamis cukup diganti resources nya saja
                    if (pwEmpPIM == pwEdit)
                    {
                        // Redirect to innerDataMC page with the new data's ID
                        return RedirectToAction("innerDataMC", new { main_id = main_id });
                    }
                    else
                    {
                        // tampilkan error berikut jika user salah memasukkan password
                        TempData["Error"] = "Failed: Wrong password failed to Edit!";
                        return RedirectToAction("firstPage", new { shiftEmp = myShift, dateFilter = myDate });
                    }

                }
            }
        }
        //public JsonResult NewMachinePartnumber_getCT(string partnumber)
        //{
        //    var partnumber_pim = ssc_db.pim_part_number_list.FirstOrDefault(m => m.PART == partnumber);

        //    if (partnumber_pim != null)
        //    {
        //        // Mengembalikan hasil dari pim_part_number_list dalam format JSON
        //        return Json(partnumber_pim, JsonRequestBehavior.AllowGet);
        //    }

        //    // Jika tidak ditemukan di pim_part_number_list, coba cari di MDS_PART_NUMBER_LIST
        //    var partnumber_mds = pn_db.MDS_PART_NUMBER_LIST.FirstOrDefault(m => m.PN == partnumber);

        //    if (partnumber_mds != null)
        //    {
        //        // Mengembalikan hasil dari MDS_PART_NUMBER_LIST dalam format JSON
        //        return Json(partnumber_mds, JsonRequestBehavior.AllowGet);
        //    }

        //    // Jika tidak ditemukan di kedua tabel, mengembalikan null dalam format JSON
        //    return Json(null, JsonRequestBehavior.AllowGet);
        //}



        // ====================================================================== //
        // method ini untuk menampilkan value sekaligus membawa value pada saat 
        public ActionResult newMachinePartnumber(int id)
        {
            // cara data berdasarkan id yang dikirim dan simpan ke dalam variable
            var data = ssc_db.PIM_SSC_MAIN.Where(x => x.main_id == id).FirstOrDefault();
            var data_calc = ssc_db.PIM_SSC_CALCULATION.Where(x => x.main_id_tc == id).FirstOrDefault();

            // jika data utama tidak ditemukan maka arahkan user ke halaman error
            if (data == null)
            {
                return HttpNotFound();
            }

            // jika data kalkulasi tersedia maka simpan value act ouput pcs ke dalam viewbag
            if (data_calc != null)
            {
                ViewBag.total_output_pcs = data_calc.total_output_pcs;
            }

            // Data-data tertentu yang harus disimpan ke dalam VIEWBAG
            ViewBag.main_id = data.main_id;
            ViewBag.shift_ = data.shift_;
            ViewBag.date_ = data.date_;
            ViewBag.we_ = data.we_;
            ViewBag.op_kpk = data.op_kpk;
            ViewBag.op_name = data.op_name;
            ViewBag.no_machine = data.no_machine;
            ViewBag.partnumber = data.partnumber;
            ViewBag.pn_set = data.pn_set;
            ViewBag.pn_cav = data.pn_cav;
            ViewBag.total_actoutput = data.total_actoutput;
            ViewBag.sch = data.sch;
            ViewBag.mc_area = data.mc_area;
            ViewBag.part_pp = data.part_pp;
            ViewBag.pn_notes = data.pn_notes;

            // tampilan user dengan membawa data dari variable 'data'
            return View(data);

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult newMachinePartnumber(PIM_SSC_MAIN model)
        {
            // simpan session date di dalam variable myDate, karena value ini akan selalu dipakai 
            DateTime myDate = (DateTime)System.Web.HttpContext.Current.Session["myDate"];

            //var partnumber = pn_db.MDS_PART_NUMBER_LIST.FirstOrDefault(m => m.PN == model.partnumber);

            var user_op_kpk = ssc_db.pim_user_kpk.FirstOrDefault(m => m.op_kpk == model.op_kpk);

            // ----------- --------- WEEK ENDING ---------- ---------- //
            DateTime dataDate1 = model.date_.Value;
            DateTime weekEnding1 = dataDate1.AddDays(6 - (int)dataDate1.DayOfWeek);

            // testing value weekEnding, pakai function ToShortDateString untuk panggil tanggalnya saja tidak perlu time nya
            ViewBag.WeekEnding1 = weekEnding1.ToShortDateString();
            ViewBag.pn_set = model.pn_set;
            // -------------------------------------------------------- //

            // jika kpk user tidak ditemukan maka kirim notif error karena kpk operator dan alihkan halaman tetao di halaman terkini, dan membawa ID
            if (user_op_kpk == null)
            {
                TempData["Error_add_new"] = "Error: Mohon check kembali KPK Operator";
                return RedirectToAction("newMachinePartnumber", new { id = model.main_id });
            }
            // jika nilai cavity kurang dari nilai set, maka setting error karena tidak mungkin cav lebih kecil, dan membawa ID
            else if (model.pn_cav < model.pn_set)
            {
                TempData["Error_add_new"] = "Error: Nilai CAVITY tidak boleh lebih kecil dari nilai SET.";
                return RedirectToAction("newMachinePartnumber", new { id = model.main_id });
            }
            else
            {
                // Assign the value of weekEnding1 to the WeekEnding property of the model
                // function ToShortDateString untuk panggil tanggalnya saja tidak perlu time nya
                model.we_ = weekEnding1.ToShortDateString();

                // jgn pakai DateTime.UtcNow karena filter data bisa kacau
                // model.date_ = DateTime.UtcNow;

                model.date_ = myDate;
                model.op_name = user_op_kpk.op_name;

                // masukkan data family/set dari table pim_part_number_list ke pim_user_output
                // var partnumber_mds = pn_db.MDS_PART_NUMBER_LIST.FirstOrDefault(m => m.PN == model.partnumber);
                var partnumber_pim = ssc_db.pim_part_number_list.FirstOrDefault(m => m.PART == model.partnumber);
                if (partnumber_pim != null)
                {
                    model.pn_ct = partnumber_pim.CT;
                    model.mold = partnumber_pim.MOLD;
                    ssc_db.SaveChanges();
                }
                //else if (partnumber != null)
                //{
                //    // simpan value ke DB jika ada hasil pencarian
                //    model.pn_ct = partnumber.CT;
                //    model.mold = partnumber.MOLD;
                //    ssc_db.SaveChanges();
                //}

                // untuk menyimpan value-value baru
                ssc_db.Entry(model).State = EntityState.Modified;
                ssc_db.SaveChanges();

                // cari data pada database table main berdasarakan main_id nya
                var data_main = ssc_db.PIM_SSC_MAIN.FirstOrDefault(m => m.main_id == model.main_id);

                // jika nilai set null atau kosong maka isi otomatis nilainya sama dengan cavity, agar pengalioutput == 1
                if ((model.pn_set == null || model.pn_set == 0))
                {
                    model.pn_set = data_main.pn_cav;
                    ssc_db.SaveChanges();
                }
                // Jika value CAV dan SET tersedia, maka lakukan kalkulasi untuk simpan value pengali output
                if ((model.pn_set != null && model.pn_set > 0) && (model.pn_cav != null && model.pn_cav > 0))
                {
                    model.pengalioutput = (int)Math.Round((double)model.pn_cav / (double)model.pn_set, 2);
                    ssc_db.SaveChanges();
                }
                else
                {
                    model.pengalioutput = 0;
                    ssc_db.SaveChanges();
                }


                // Create a new instance of PIM_SSC_CALCULATION and assign the main_id
                var data_calc = ssc_db.PIM_SSC_CALCULATION.Where(x => x.main_id_tc == model.main_id).FirstOrDefault();
                if (data_calc == null)
                {
                    var newCalculation = new PIM_SSC_CALCULATION
                    {
                        main_id_tc = model.main_id
                    };

                    // tambahkan data ke database
                    ssc_db.PIM_SSC_CALCULATION.Add(newCalculation);

                    // Save changes to the database
                    ssc_db.SaveChanges();
                }
                // CHECK STD OUTPUT
                else if (data_calc.pn_operatingtime != null)
                {
                    //UpdatePnStandardOutput();
                    data_calc.pn_stdoutput = (3600 / data_main.pn_ct) * data_main.pn_cav * (data_calc.pn_operatingtime / 60);
                    ssc_db.SaveChanges();
                }

                // update nilai output
                var data_o = ssc_db.PIM_SSC_OUTPUT.Where(x => x.main_id_o == model.main_id).ToList();
                if (data_o != null)
                {
                    foreach (var output in data_o)
                    {
                        output.actoutput_pcs = output.actoutput * data_main.pengalioutput;
                    }

                    data_main.total_actoutput = data_o.Sum(d => d.actoutput);
                    ssc_db.SaveChanges();
                }


                // >>>>>>> MASUKKAN nilai OUTPUT PER PCS <<<<<<<<< //
                // Update total_actoutput di data_main
                if (data_main != null && data_calc != null)
                {
                    // Calculate the sum of actoutput_pcs// Calculate the sum of actoutput_pcs
                    int? totalOutputPcs = ssc_db.PIM_SSC_OUTPUT
                        .Where(x => x.main_id_o == model.main_id)
                        .Sum(x => x.actoutput_pcs);

                    // Assign the totalOutputPcs value to data_calc.total_output_pcs after handling the nullable value
                    data_calc.total_output_pcs = totalOutputPcs.HasValue ? totalOutputPcs.Value : 0;
                    ssc_db.SaveChanges();
                }
                else
                {
                    //data_calc.total_output_pcs = 0;
                    ssc_db.SaveChanges();
                }
                // Update user output data
                data_calc = ssc_db.PIM_SSC_CALCULATION.FirstOrDefault(x => x.main_id_tc == model.main_id);
                if (data_calc != null)
                {
                    // Calculate total rj_quantity for the user
                    var totalRj = ssc_db.PIM_SSC_O_REJECT.Where(x => x.main_id_rj == model.main_id).Sum(x => x.reject_qty);
                    // Update totalRj and actual_good_output for the user
                    data_calc.totalRJ = totalRj;
                    ssc_db.SaveChanges();

                    data_calc.total_good_output = (data_calc.total_output_pcs ?? 0) - (data_calc.totalRJ ?? 0);
                    ssc_db.SaveChanges();
                }

                ViewBag.Message = "Data Insert Successfully!";

                // Perhitungan di bawah ini untuk menghitung total timw
                // perhitungan ini sangat sensitif, harus hati-hati dalam mengedit algoritmanya
                // sudah dicoba untuk lebih clear dalam penulisan kode program, namun menyebabkan error lainnya
                // backup logic code berikut, sebelum merekontruksi kode programnya
                // logic total time ini dibuat hampir satu minggu

                // DECLARE VARIABLE ACTUAL OUTPUT PLAN PER HOUR
                int actual_plan1 = ssc_db.PIM_SSC_OUTPUT.FirstOrDefault(x => x.main_id_o == model.main_id && x.hour_counter == 1)?.actoutput ?? 0;
                int actual_plan2 = ssc_db.PIM_SSC_OUTPUT.FirstOrDefault(x => x.main_id_o == model.main_id && x.hour_counter == 2)?.actoutput ?? 0;
                int actual_plan3 = ssc_db.PIM_SSC_OUTPUT.FirstOrDefault(x => x.main_id_o == model.main_id && x.hour_counter == 3)?.actoutput ?? 0;
                int actual_plan4 = ssc_db.PIM_SSC_OUTPUT.FirstOrDefault(x => x.main_id_o == model.main_id && x.hour_counter == 4)?.actoutput ?? 0;
                int actual_plan5 = ssc_db.PIM_SSC_OUTPUT.FirstOrDefault(x => x.main_id_o == model.main_id && x.hour_counter == 5)?.actoutput ?? 0;
                int actual_plan6 = ssc_db.PIM_SSC_OUTPUT.FirstOrDefault(x => x.main_id_o == model.main_id && x.hour_counter == 6)?.actoutput ?? 0;
                int actual_plan7 = ssc_db.PIM_SSC_OUTPUT.FirstOrDefault(x => x.main_id_o == model.main_id && x.hour_counter == 7)?.actoutput ?? 0;
                int actual_plan8 = ssc_db.PIM_SSC_OUTPUT.FirstOrDefault(x => x.main_id_o == model.main_id && x.hour_counter == 8)?.actoutput ?? 0;
                int actual_plan9 = ssc_db.PIM_SSC_OUTPUT.FirstOrDefault(x => x.main_id_o == model.main_id && x.hour_counter == 9)?.actoutput ?? 0;

                // DECLARE VARIABLE DOWNTIME PER HOUR
                var current_dt_1 = ssc_db.PIM_SSC_O_DOWNTIME.Where(x => x.main_id_dt == model.main_id && x.hour_counter == 1).ToList();
                var current_dt_2 = ssc_db.PIM_SSC_O_DOWNTIME.Where(x => x.main_id_dt == model.main_id && x.hour_counter == 2).ToList();
                var current_dt_3 = ssc_db.PIM_SSC_O_DOWNTIME.Where(x => x.main_id_dt == model.main_id && x.hour_counter == 3).ToList();
                var current_dt_4 = ssc_db.PIM_SSC_O_DOWNTIME.Where(x => x.main_id_dt == model.main_id && x.hour_counter == 4).ToList();
                var current_dt_5 = ssc_db.PIM_SSC_O_DOWNTIME.Where(x => x.main_id_dt == model.main_id && x.hour_counter == 5).ToList();
                var current_dt_6 = ssc_db.PIM_SSC_O_DOWNTIME.Where(x => x.main_id_dt == model.main_id && x.hour_counter == 6).ToList();
                var current_dt_7 = ssc_db.PIM_SSC_O_DOWNTIME.Where(x => x.main_id_dt == model.main_id && x.hour_counter == 7).ToList();
                var current_dt_8 = ssc_db.PIM_SSC_O_DOWNTIME.Where(x => x.main_id_dt == model.main_id && x.hour_counter == 8).ToList();
                var current_dt_9 = ssc_db.PIM_SSC_O_DOWNTIME.Where(x => x.main_id_dt == model.main_id && x.hour_counter == 9).ToList();


                // Jika per plan ada output, maka tambahkan waktu tersebut per plan output
                // jika per plan ada downtime, maka tambahakan waktu tersetbut tiap downtime
                // jika tidak ada output dan tidak ada downtime, isi not running, agar waktu totaltime dapat menyesuaikan
                // actual_plan = output per plan
                // current_dt = downtime per plan



                // check jika data ada / tidak sama dengan NULL 
                if (data_main != null && data_calc != null)
                {
                    int totaltime1 = 0;
                    if (model.shift_ == 2)
                    {
                        if (current_dt_1.Any())
                        {
                            totaltime1 = 50;
                        }
                        else if (actual_plan1 != 0)
                        {
                            totaltime1 = 50;
                        }
                        else
                        {
                            totaltime1 = 0;
                        }
                    }
                    else
                    {
                        if (current_dt_1.Any())
                        {
                            totaltime1 = 20;
                        }
                        else if (actual_plan1 != 0)
                        {
                            totaltime1 = 20;
                        }
                        else
                        {
                            totaltime1 = 0;
                        }
                    }
                    int totaltime2 = 0;
                    if (current_dt_2.Any())
                    {
                        totaltime2 = 60;
                    }
                    else if (actual_plan2 != 0)
                    {
                        totaltime2 = 60;
                    }
                    else
                    {
                        totaltime2 = 0;
                    }

                    int totaltime3 = 0;
                    if (current_dt_3.Any())
                    {
                        totaltime3 = 60;
                    }
                    else if (actual_plan3 != 0)
                    {
                        totaltime3 = 60;
                    }
                    else
                    {
                        totaltime3 = 0;
                    }

                    int totaltime4 = 0;
                    if (current_dt_4.Any())
                    {
                        totaltime4 = 60;
                    }
                    else if (actual_plan4 != 0)
                    {
                        totaltime4 = 60;
                    }
                    else
                    {
                        totaltime4 = 0;
                    }

                    int totaltime5 = 0;
                    if (current_dt_5.Any())
                    {
                        totaltime5 = 60;
                    }
                    else if (actual_plan5 != 0)
                    {
                        totaltime5 = 60;
                    }
                    else
                    {
                        totaltime5 = 0;
                    }

                    int totaltime6 = 0;
                    if (current_dt_6.Any())
                    {
                        totaltime6 = 60;
                    }
                    else if (actual_plan6 != 0)
                    {
                        totaltime6 = 60;
                    }
                    else
                    {
                        totaltime6 = 0;
                    }

                    int totaltime7 = 0;
                    if (current_dt_7.Any())
                    {
                        totaltime7 = 60;
                    }
                    else if (actual_plan7 != 0)
                    {
                        totaltime7 = 60;
                    }
                    else
                    {
                        totaltime7 = 0;
                    }

                    int totaltime8 = 0;
                    if (current_dt_8.Any())
                    {
                        if (model.shift_ == 3)
                        {
                            totaltime8 = 40;
                        }
                        else
                        {
                            totaltime8 = 60;
                        }
                    }
                    else if (actual_plan8 != 0)
                    {
                        if (model.shift_ == 3)
                        {
                            totaltime8 = 40;
                        }
                        else
                        {
                            totaltime8 = 60;
                        }
                    }
                    else
                    {
                        totaltime8 = 0;
                    }



                    if (model.shift_ == 3)
                    {
                        data_calc.total_time = totaltime1 + totaltime2 + totaltime3 + totaltime4 + totaltime5 + totaltime6 + totaltime7 + totaltime8;

                        ssc_db.SaveChanges();

                        //UpdatePnOperatingTime();

                        //UpdatePnStandardOutput();
                    }
                    else
                    {
                        int totaltime9 = 0;
                        if (model.shift_ == 2)
                        {
                            if (current_dt_9.Any())
                            {
                                totaltime9 = 40;
                            }
                            else if (actual_plan9 != 0)
                            {
                                totaltime9 = 40;
                            }
                            else
                            {
                                totaltime9 = 0;
                            }

                        }
                        else
                        {
                            // shift 1
                            if (current_dt_9.Any())
                            {
                                totaltime9 = 70;
                            }
                            else if (actual_plan9 != 0)
                            {
                                totaltime9 = 70;
                            }
                            else
                            {
                                totaltime9 = 0;
                            }
                        }

                        data_calc.total_time = totaltime1 + totaltime2 + totaltime3 + totaltime4 + totaltime5 + totaltime6 + totaltime7 + totaltime8 + totaltime9;
                        ssc_db.SaveChanges();
                        UpdatePnOperatingTime();
                        //UpdatePnStandardOutput();

                    }

                }


                // redirect ke halaman UPDATE DATA dengan ID data yang baru ditambahkan
                return RedirectToAction("innerDataMC", new { main_id = model.main_id });
            }
        }


        // ---------------------------------------------------------------------------------------------------------- //
        // Menampilkan isi data SSC yang sudah di lock, ditampilkan sesuai ID

        public ActionResult innerDataMC_locked(int main_id)
        {
            return View();
        }


        public ActionResult innerDataMC(int main_id)
        {
            //DateTime dateFilter = (DateTime)System.Web.HttpContext.Current.Session["myDate"];
            DateTime currentDate = DateTime.Now;


            // MAIN ID
            ViewBag.main_id = main_id;

            var data_main = ssc_db.PIM_SSC_MAIN.Where(x => x.main_id == main_id).FirstOrDefault();

            // -------------------------------READ DATA - KPK OPERATOR -------------------------------- //
            // Cari data kpk operator dan read data di sini,
            // var kpk_operator = ssc_db.pim_user_kpk.Where(x => x.op_kpk == data_main.op_kpk).FirstOrDefault();
            ViewBag.op_kpk = data_main.op_kpk;

            // -------------------------------READ DATA - PART NUMBER OFFICIAL-------------------------------- //
            // Cari data partnumber dari db partnumber_official dan read data di sini,
            // Search berdasarkan partnumber data yang ada di var data.partnumber
            //var data_pn_MDS = pn_db.MDS_PART_NUMBER_LIST.Where(x => x.PN == data_main.partnumber).FirstOrDefault();

            var data_pn = ssc_db.pim_part_number_list.Where(x => x.PART == data_main.partnumber).FirstOrDefault();
            if (data_pn == null)
            {
                // tampilkan nilai kosong jika tidak ada hasil pencarian
                ViewBag.DESCR = null;
                ViewBag.MOLD = null;
                ViewBag.MATERIAL = null;
                ViewBag.CAV = 0;
                ViewBag.CT = 0;
                ViewBag.SHOT_WEIGHT = null;
                ViewBag.PN_COLORANT = null;
            }
            else
            {
                // lakukan aksi jika ada hasil pencarian
                ViewBag.DESCR = "Not Found";
                ViewBag.MOLD = data_pn.MOLD;
                ViewBag.MATERIAL = "Not Found";
                ViewBag.CAV = "Not Found";
                ViewBag.CT = data_pn.CT;
                ViewBag.SHOT_WEIGHT = "Not Found";
                ViewBag.PN_COLORANT = "Not Found";

                //if (data_pn_MDS == null)
                //{
                //    // lakukan aksi jika ada hasil pencarian
                //    ViewBag.DESCR = "Not Found";
                //    ViewBag.MOLD = data_pn.MOLD;
                //    ViewBag.MATERIAL = "Not Found";
                //    ViewBag.CAV = "Not Found";
                //    ViewBag.CT = data_pn.CT;
                //    ViewBag.SHOT_WEIGHT = "Not Found";
                //    ViewBag.PN_COLORANT = "Not Found";
                //}
                //else
                //{
                //    // lakukan aksi jika ada hasil pencarian
                //    ViewBag.DESCR = data_pn_MDS.DESCR;
                //    ViewBag.MOLD = data_pn_MDS.MOLD;
                //    ViewBag.MATERIAL = data_pn_MDS.MATERIAL;
                //    ViewBag.CAV = data_pn_MDS.CAV;
                //    ViewBag.CT = data_main.pn_ct; // CT yang tersimpan di DB
                //    ViewBag.SHOT_WEIGHT = data_pn_MDS.SHOT_WEIGHT;
                //    ViewBag.PN_COLORANT = data_pn_MDS.PN_COLORANT;
                //}
            }

            // BINARY SEARCH
            // DECLARE VARIABLE ACTUAL PLAN
            var actual_plan1 = ssc_db.PIM_SSC_OUTPUT.Where(x => x.main_id_o == main_id && x.hour_counter == 1).FirstOrDefault();
            var actual_plan2 = ssc_db.PIM_SSC_OUTPUT.Where(x => x.main_id_o == main_id && x.hour_counter == 2).FirstOrDefault();
            var actual_plan3 = ssc_db.PIM_SSC_OUTPUT.Where(x => x.main_id_o == main_id && x.hour_counter == 3).FirstOrDefault();
            var actual_plan4 = ssc_db.PIM_SSC_OUTPUT.Where(x => x.main_id_o == main_id && x.hour_counter == 4).FirstOrDefault();
            var actual_plan5 = ssc_db.PIM_SSC_OUTPUT.Where(x => x.main_id_o == main_id && x.hour_counter == 5).FirstOrDefault();
            var actual_plan6 = ssc_db.PIM_SSC_OUTPUT.Where(x => x.main_id_o == main_id && x.hour_counter == 6).FirstOrDefault();
            var actual_plan7 = ssc_db.PIM_SSC_OUTPUT.Where(x => x.main_id_o == main_id && x.hour_counter == 7).FirstOrDefault();
            var actual_plan8 = ssc_db.PIM_SSC_OUTPUT.Where(x => x.main_id_o == main_id && x.hour_counter == 8).FirstOrDefault();
            var actual_plan9 = ssc_db.PIM_SSC_OUTPUT.Where(x => x.main_id_o == main_id && x.hour_counter == 9).FirstOrDefault();

            // ACTUAL PLAN
            // If the object is not null, assign its actoutput value to ViewBag.actual_plan1.
            // Otherwise, we set ViewBag.actual_plan1 to 0 as the default value. 
            ViewBag.actual_plan1 = actual_plan1 != null ? actual_plan1.actoutput : 0;
            ViewBag.actual_plan2 = actual_plan2 != null ? actual_plan2.actoutput : 0;
            ViewBag.actual_plan3 = actual_plan3 != null ? actual_plan3.actoutput : 0;
            ViewBag.actual_plan4 = actual_plan4 != null ? actual_plan4.actoutput : 0;
            ViewBag.actual_plan5 = actual_plan5 != null ? actual_plan5.actoutput : 0;
            ViewBag.actual_plan6 = actual_plan6 != null ? actual_plan6.actoutput : 0;
            ViewBag.actual_plan7 = actual_plan7 != null ? actual_plan7.actoutput : 0;
            ViewBag.actual_plan8 = actual_plan8 != null ? actual_plan8.actoutput : 0;
            ViewBag.actual_plan9 = actual_plan9 != null ? actual_plan9.actoutput : 0;

            // Hour Counter per Plan
            //
            ViewBag.hourPlan_o1 = 1;
            ViewBag.hourPlan_o2 = 2;
            ViewBag.hourPlan_o3 = 3;
            ViewBag.hourPlan_o4 = 4;
            ViewBag.hourPlan_o5 = 5;
            ViewBag.hourPlan_o6 = 6;
            ViewBag.hourPlan_o7 = 7;
            ViewBag.hourPlan_o8 = 8;
            ViewBag.hourPlan_o9 = 9;


            // ---------------------------------------------------------------------------------------------- //

            // TESTING - Date conversion
            string hari = DateTime.Now.ToString("dddd"); // nama hari
            string tgl = DateTime.Now.ToString("dd"); // tanggal
            string bulan = DateTime.Now.ToString("MMMM"); // nama bulan
            string tahun = DateTime.Now.ToString("yyyy"); // tahun
            string hasil = hari + ", " + tgl + " " + bulan + " " + tahun; // hasil akhir

            ViewBag.dateNow = hasil;

            // GENERAL
            // GENERAL VIEWBAG
            ViewBag.main_id = data_main.main_id;
            ViewBag.shift = data_main.shift_;
            ViewBag.date = data_main.date_;
            ViewBag.we_ = data_main.we_;
            ViewBag.op_kpk = data_main.op_kpk;
            ViewBag.op_name = data_main.op_name;
            ViewBag.shiftly_target = data_main.sch;
            ViewBag.total_actoutput = data_main.total_actoutput;
            ViewBag.no_machine = data_main.no_machine;
            ViewBag.partnumber = data_main.partnumber;
            ViewBag.pn_set = data_main.pn_set;
            ViewBag.pn_cav = data_main.pn_cav;
            ViewBag.pengalioutput = data_main.pengalioutput;
            ViewBag.pn_notes = data_main.pn_notes;
            ViewBag.part_pp = data_main.part_pp;

            // ACTUAL OUTPUT
            var data_o = ssc_db.PIM_SSC_OUTPUT.Where(x => x.main_id_o == data_main.main_id).FirstOrDefault();

            // jika ada data yang ditemukan, tampilkan view tes_input dengan menggunakan data tersebut
            if (data_o != null)
            {
                ViewBag.hourcounter = data_o.hour_counter;
                ViewBag.actual_output = data_o.actoutput;
                ViewBag.actual_output_pcs = data_o.actoutput_pcs;
            }

            // Calculation OUTPUT
            var data_calc = ssc_db.PIM_SSC_CALCULATION.Where(x => x.main_id_tc == data_main.main_id).FirstOrDefault();

            if (data_calc != null)
            {
                ViewBag.total_output_pcs = data_calc.total_output_pcs;
                ViewBag.total_good_output = data_calc.total_good_output;
                ViewBag.totalDT = data_calc.totalDT;
                ViewBag.totalRJ = data_calc.totalRJ;
            }


            if (data_main != null && data_main.shift_.HasValue)
            {
                var shift = data_main.shift_.Value;
                if (shift == 3)
                {
                    // Handle other shift values here - SHIFT 3
                    ViewBag.time1 = "15:40 - 16:00"; ViewBag.time2 = "16:00 - 17:00";
                    ViewBag.time3 = "17:00 - 18:00"; ViewBag.time4 = "18:00 - 19:00";
                    ViewBag.time5 = "19:00 - 20:00"; ViewBag.time6 = "20:00 - 21:00";
                    ViewBag.time7 = "21:00 - 22:00"; ViewBag.time8 = "22:00 - 22:40";
                    ViewBag.time9 = "Not Available";
                }
                else if (shift == 1)
                {
                    // Handle other shift values here - SHIFT 1
                    ViewBag.time1 = "22:40 - 23:00"; ViewBag.time2 = "23:00 - 24:00";
                    ViewBag.time3 = "24:00 - 01:00"; ViewBag.time4 = "01:00 - 02:00";
                    ViewBag.time5 = "02:00 - 03:00"; ViewBag.time6 = "03:00 - 04:00";
                    ViewBag.time7 = "04:00 - 05:00"; ViewBag.time8 = "05:00 - 06:00";
                    ViewBag.time9 = "06:00 - 07:10";
                }
                else
                {
                    // Handle other shift values here - SHIFT 2 (Default)
                    ViewBag.time1 = "07:10 - 08:00"; ViewBag.time2 = "08:00 - 09:00";
                    ViewBag.time3 = "09:00 - 10:00"; ViewBag.time4 = "10:00 - 11:00";
                    ViewBag.time5 = "11:00 - 12:00"; ViewBag.time6 = "12:00 - 13:00";
                    ViewBag.time7 = "13:00 - 14:00"; ViewBag.time8 = "14:00 - 15:00";
                    ViewBag.time9 = "15:00 - 15:40";
                }
            }
            else
            {
                ViewBag.time1 = "Shift not available";
                ViewBag.time2 = "Shift not available";
                ViewBag.time3 = "Shift not available";
                ViewBag.time4 = "Shift not available";
                ViewBag.time5 = "Shift not available";
                ViewBag.time6 = "Shift not available";
                ViewBag.time7 = "Shift not available";
                ViewBag.time8 = "Shift not available";
                ViewBag.time9 = "Shift not available";
            }


            var table_data_PIM = new pimListData
            {
                // database user output
                PIM_SSC_MAINs = ssc_db.PIM_SSC_MAIN.Where(x => x.main_id == main_id && x.op_kpk != null).ToList(),

                // Static Data - Downtime Code sortir sesuai id
                pim_Downtimes = ssc_db.pim_downtime.OrderBy(x => x.id).ToList(),

                // Static Data - Reject Code sortir sesuai remarks
                pim_Rejects = ssc_db.pim_reject.OrderBy(x => x.id).ToList(),

                // read data - relationship table reject with user 
                VO_REJECTs = ssc_db.VO_REJECT.Where(x => x.main_id_rj == main_id).OrderBy(x => x.hour_counter).ThenBy(x => x.rj_remarks).ToList(),

                // read data - relationship table reject with user 
                VO_DOWNTIMEs = ssc_db.VO_DOWNTIME.Where(x => x.main_id_dt == main_id).OrderBy(x => x.hour_counter).ThenBy(x => x.dt_start).ThenBy(x => x.dt_remarks).ToList(),
            };


            return View(table_data_PIM);

            //if (currentDate > dateFilter)
            //{
            //    return RedirectToAction("innerDataMC_locked", new { id = main_id });
            //}
            //else
            //{
            //    return View(table_data);

            //}

        }


        // ---------------------------------------------------------------------------------------------------------- //
        // UPDATE DATA sesuai ID
        [HttpPost]
        public ActionResult innerDataMC_2(PIM_SSC_OUTPUT model, int main_id, string pn_notes, string part_pp, int actual_plan1 = 0, int actual_plan2 = 0, int actual_plan3 = 0, int actual_plan4 = 0,
            int actual_plan5 = 0, int actual_plan6 = 0, int actual_plan7 = 0, int actual_plan8 = 0, int actual_plan9 = 0)
        {
            if (ModelState.IsValid)
            {
                // SESSION
                //int myShift = (int)System.Web.HttpContext.Current.Session["myShift"];
                //DateTime myDate = (DateTime)System.Web.HttpContext.Current.Session["myDate"];
                // -------------------------------INSERT DATA - OUTPUT USER -------------------------------- //

                // data table main
                var data_main = ssc_db.PIM_SSC_MAIN.Where(x => x.main_id == main_id).FirstOrDefault();

                int myShift = (int)data_main.shift_;
                DateTime myDate = (DateTime)data_main.date_;

                if (data_main != null)
                {
                    data_main.part_pp = part_pp;
                    ssc_db.SaveChanges();


                }

                // Create a new instance of PIM_SSC_CALCULATION and assign the main_id
                var data_calc = ssc_db.PIM_SSC_CALCULATION.Where(x => x.main_id_tc == main_id).FirstOrDefault();
                if (data_calc == null)
                {
                    var newCalculation = new PIM_SSC_CALCULATION
                    {
                        main_id_tc = main_id
                    };

                    // tambahkan data ke database
                    ssc_db.PIM_SSC_CALCULATION.Add(newCalculation);

                    // Save changes to the database
                    ssc_db.SaveChanges();


                }
                // data table output

                //===================================================================================//
                // NEED IMPROVEMENT 
                //
                // data table output
                var data_output = ssc_db.PIM_SSC_OUTPUT.Where(x => x.main_id_o == main_id).ToList();
                // hapus data jika existing
                if (data_output.Any())
                {
                    ssc_db.PIM_SSC_OUTPUT.RemoveRange(data_output);
                    ssc_db.SaveChanges();
                }

                // add new data tiap actual_plan
                int[] actualPlans = new int[] { actual_plan1, actual_plan2, actual_plan3, actual_plan4, actual_plan5, actual_plan6, actual_plan7, actual_plan8, actual_plan9 };
                for (int hourCounter = 1; hourCounter <= 9; hourCounter++)
                {
                    int actualPlan = actualPlans[hourCounter - 1];
                    if (actualPlan != 0)
                    {
                        ssc_db.PIM_SSC_OUTPUT.Add(new PIM_SSC_OUTPUT
                        {
                            main_id_o = main_id,
                            hour_counter = hourCounter,
                            actoutput = actualPlan,
                            actoutput_pcs = actualPlan * data_main.pengalioutput
                        });
                        ssc_db.SaveChanges();
                    }
                }

                // var sql_test = sql statement >> text dimasukkan ke variable >> execute

                // jika actual_plan == 0 maka otomatis delete reject yang ada di jam tersebut
                for (int hourCounter = 1; hourCounter <= 9; hourCounter++)
                {
                    int actualPlan = actualPlans[hourCounter - 1];
                    if (actualPlan == 0)
                    {
                        var dataReject = ssc_db.PIM_SSC_O_REJECT.Where(x => x.main_id_rj == main_id && x.hour_counter == hourCounter).ToList();
                        if (dataReject.Any())
                        {
                            ssc_db.PIM_SSC_O_REJECT.RemoveRange(dataReject);
                            ssc_db.SaveChanges();
                        }
                    }
                }




                // >>>>>>> OUTPUT PER PCS <<<<<<<<< //
                // Update total_actoutput di data_main

                data_main.total_actoutput = ssc_db.PIM_SSC_OUTPUT.Where(x => x.main_id_o == main_id).Sum(d => d.actoutput);
                ssc_db.SaveChanges();

                // panggil lagi untuk memastikan main_id_tc sudah diisi dengan value dari main_id di atas agar tidak null
                data_calc = ssc_db.PIM_SSC_CALCULATION.Where(x => x.main_id_tc == main_id).FirstOrDefault();

                if (data_main != null && data_calc != null)
                {
                    // Calculate the sum of actoutput_pcs// Calculate the sum of actoutput_pcs
                    int? totalOutputPcs = ssc_db.PIM_SSC_OUTPUT
                        .Where(x => x.main_id_o == main_id)
                        .Sum(x => x.actoutput_pcs);

                    // Assign the totalOutputPcs value to data_calc.total_output_pcs after handling the nullable value
                    data_calc.total_output_pcs = totalOutputPcs.HasValue ? totalOutputPcs.Value : 0;
                    ssc_db.SaveChanges();
                }
                else
                {
                    data_calc.total_output_pcs = 0;
                    ssc_db.SaveChanges();
                }

                // ========================================== //

                // Update user output data
                data_calc = ssc_db.PIM_SSC_CALCULATION.FirstOrDefault(x => x.main_id_tc == main_id);

                if (data_calc != null)
                {
                    // Calculate total rj_quantity for the user
                    var totalRj = ssc_db.PIM_SSC_O_REJECT.Where(x => x.main_id_rj == main_id).Sum(x => x.reject_qty);
                    // Update totalRj and actual_good_output for the user
                    data_calc.totalRJ = totalRj;
                    ssc_db.SaveChanges();

                    data_calc.total_good_output = (data_calc.total_output_pcs ?? 0) - (data_calc.totalRJ ?? 0);
                    ssc_db.SaveChanges();

                }




                // DECLARE VARIABLE ACTUAL OUTPUT PLAN PER HOUR
                actual_plan1 = ssc_db.PIM_SSC_OUTPUT.FirstOrDefault(x => x.main_id_o == main_id && x.hour_counter == 1)?.actoutput ?? 0;
                actual_plan2 = ssc_db.PIM_SSC_OUTPUT.FirstOrDefault(x => x.main_id_o == main_id && x.hour_counter == 2)?.actoutput ?? 0;
                actual_plan3 = ssc_db.PIM_SSC_OUTPUT.FirstOrDefault(x => x.main_id_o == main_id && x.hour_counter == 3)?.actoutput ?? 0;
                actual_plan4 = ssc_db.PIM_SSC_OUTPUT.FirstOrDefault(x => x.main_id_o == main_id && x.hour_counter == 4)?.actoutput ?? 0;
                actual_plan5 = ssc_db.PIM_SSC_OUTPUT.FirstOrDefault(x => x.main_id_o == main_id && x.hour_counter == 5)?.actoutput ?? 0;
                actual_plan6 = ssc_db.PIM_SSC_OUTPUT.FirstOrDefault(x => x.main_id_o == main_id && x.hour_counter == 6)?.actoutput ?? 0;
                actual_plan7 = ssc_db.PIM_SSC_OUTPUT.FirstOrDefault(x => x.main_id_o == main_id && x.hour_counter == 7)?.actoutput ?? 0;
                actual_plan8 = ssc_db.PIM_SSC_OUTPUT.FirstOrDefault(x => x.main_id_o == main_id && x.hour_counter == 8)?.actoutput ?? 0;
                actual_plan9 = ssc_db.PIM_SSC_OUTPUT.FirstOrDefault(x => x.main_id_o == main_id && x.hour_counter == 9)?.actoutput ?? 0;

                // DECLARE VARIABLE DOWNTIME PER HOUR
                var current_dt_1 = ssc_db.PIM_SSC_O_DOWNTIME.Where(x => x.main_id_dt == main_id && x.hour_counter == 1).ToList();
                var current_dt_2 = ssc_db.PIM_SSC_O_DOWNTIME.Where(x => x.main_id_dt == main_id && x.hour_counter == 2).ToList();
                var current_dt_3 = ssc_db.PIM_SSC_O_DOWNTIME.Where(x => x.main_id_dt == main_id && x.hour_counter == 3).ToList();
                var current_dt_4 = ssc_db.PIM_SSC_O_DOWNTIME.Where(x => x.main_id_dt == main_id && x.hour_counter == 4).ToList();
                var current_dt_5 = ssc_db.PIM_SSC_O_DOWNTIME.Where(x => x.main_id_dt == main_id && x.hour_counter == 5).ToList();
                var current_dt_6 = ssc_db.PIM_SSC_O_DOWNTIME.Where(x => x.main_id_dt == main_id && x.hour_counter == 6).ToList();
                var current_dt_7 = ssc_db.PIM_SSC_O_DOWNTIME.Where(x => x.main_id_dt == main_id && x.hour_counter == 7).ToList();
                var current_dt_8 = ssc_db.PIM_SSC_O_DOWNTIME.Where(x => x.main_id_dt == main_id && x.hour_counter == 8).ToList();
                var current_dt_9 = ssc_db.PIM_SSC_O_DOWNTIME.Where(x => x.main_id_dt == main_id && x.hour_counter == 9).ToList();

                //Check if there is downtime
                if (myShift != 0)
                {
                    int totaltime1 = 0;
                    if (myShift == 2)
                    {
                        if (current_dt_1.Any())
                        {
                            totaltime1 = 50;
                        }
                        else if (actual_plan1 != 0)
                        {
                            totaltime1 = 50;
                        }
                        else
                        {
                            totaltime1 = 0;
                        }
                    }
                    else
                    {
                        if (current_dt_1.Any())
                        {
                            totaltime1 = 20;
                        }
                        else if (actual_plan1 != 0)
                        {
                            totaltime1 = 20;
                        }
                        else
                        {
                            totaltime1 = 0;
                        }
                    }

                    int totaltime2 = 0;
                    if (current_dt_2.Any())
                    {
                        totaltime2 = 60;
                    }
                    else if (actual_plan2 != 0)
                    {
                        totaltime2 = 60;
                    }
                    else
                    {
                        totaltime2 = 0;
                    }

                    int totaltime3 = 0;
                    if (current_dt_3.Any())
                    {
                        totaltime3 = 60;
                    }
                    else if (actual_plan3 != 0)
                    {
                        totaltime3 = 60;
                    }
                    else
                    {
                        totaltime3 = 0;
                    }

                    int totaltime4 = 0;
                    if (current_dt_4.Any())
                    {
                        totaltime4 = 60;
                    }
                    else if (actual_plan4 != 0)
                    {
                        totaltime4 = 60;
                    }
                    else
                    {
                        totaltime4 = 0;
                    }

                    int totaltime5 = 0;
                    if (current_dt_5.Any())
                    {
                        totaltime5 = 60;
                    }
                    else if (actual_plan5 != 0)
                    {
                        totaltime5 = 60;
                    }
                    else
                    {
                        totaltime5 = 0;
                    }

                    int totaltime6 = 0;
                    if (current_dt_6.Any())
                    {
                        totaltime6 = 60;
                    }
                    else if (actual_plan6 != 0)
                    {
                        totaltime6 = 60;
                    }
                    else
                    {
                        totaltime6 = 0;
                    }

                    int totaltime7 = 0;
                    if (current_dt_7.Any())
                    {
                        totaltime7 = 60;
                    }
                    else if (actual_plan7 != 0)
                    {
                        totaltime7 = 60;
                    }
                    else
                    {
                        totaltime7 = 0;
                    }

                    int totaltime8 = 0;
                    if (current_dt_8.Any())
                    {
                        if (myShift == 3)
                        {
                            totaltime8 = 40;
                        }
                        else
                        {
                            totaltime8 = 60;
                        }

                    }
                    else if (actual_plan8 != 0)
                    {
                        if (myShift == 3)
                        {
                            totaltime8 = 40;
                        }
                        else
                        {
                            totaltime8 = 60;
                        }
                    }
                    else
                    {
                        totaltime8 = 0;
                    }

                    if (myShift == 3)
                    {
                        data_calc.total_time = totaltime1 + totaltime2 + totaltime3 + totaltime4 + totaltime5 + totaltime6 + totaltime7 + totaltime8;
                        ssc_db.SaveChanges();
                        UpdatePnOperatingTime();

                        //UpdatePnStandardOutput();
                    }
                    else
                    {
                        int totaltime9 = 0;
                        if (myShift == 2)
                        {
                            if (current_dt_9.Any())
                            {
                                totaltime9 = 40;
                            }
                            else if (actual_plan9 != 0)
                            {
                                totaltime9 = 40;
                            }
                            else
                            {
                                totaltime9 = 0;
                            }

                        }
                        else
                        {
                            // shift 1
                            if (current_dt_9.Any())
                            {
                                totaltime9 = 70;
                            }
                            else if (actual_plan9 != 0)
                            {
                                totaltime9 = 70;
                            }
                            else
                            {
                                totaltime9 = 0;
                            }
                        }

                        data_calc.total_time = totaltime1 + totaltime2 + totaltime3 + totaltime4 + totaltime5 + totaltime6 + totaltime7 + totaltime8 + totaltime9;
                        ssc_db.SaveChanges();
                        UpdatePnOperatingTime();

                        //UpdatePnStandardOutput();
                    }

                }

                if (data_main != null)
                {
                    data_main.pn_notes = pn_notes;
                    ssc_db.SaveChanges();

                }

                // UPDATE STD OUTPUT
                if (data_calc.pn_operatingtime != null)
                {
                    //UpdatePnStandardOutput();
                    data_calc.pn_stdoutput = (3600 / data_main.pn_ct) * data_main.pn_cav * (data_calc.pn_operatingtime / 60);
                    ssc_db.SaveChanges();
                }


                return RedirectToAction("innerDataMC", new { id = main_id });
            }
            //return RedirectToAction("innerDataMC", new { id = main_id });
            return View(model);
        }

        // ---------------------------------------------------------------------------------------------------------- //

        // Add New Data DOWNTIME from user to database table pim_user_output
        [HttpGet]
        public ActionResult outputDowntime()
        {

            return View();
        }
        [HttpPost]
        public ActionResult outputDowntime(PIM_SSC_O_DOWNTIME model)
        {
            // Jika jam 12 malam maka rubah jadi 23:50:59 karena untuk OEE tdk bisa detect jam 12 malam
            // Cek apakah waktu adalah 12 malam atau 00:00

            var data_calc = ssc_db.PIM_SSC_CALCULATION.FirstOrDefault(x => x.main_id_tc == model.main_id_dt);
            if (model.dt_finish?.Hours == 0 && model.dt_finish?.Minutes == 0)
            {
                // Jika iya, ubah waktu menjadi 11.59 malam
                model.dt_finish = new TimeSpan(23, 59, 59);
                ssc_db.SaveChanges();

                data_calc.total_time = data_calc.total_time - 1;
                ssc_db.SaveChanges();
                UpdatePnOperatingTime();

                //UpdatePnStandardOutput();
            }

            // ---------------------- DURATION in MINUTES ---------------------- //

            //  TimeSpan? untuk menyimpan selisih waktu (duration) antara model.dt_finish dan model.dt_start
            TimeSpan? nullableDurations = null;
            if (model.dt_finish.HasValue && model.dt_start.HasValue)
            {
                nullableDurations = model.dt_finish.Value - model.dt_start.Value;
            }

            TimeSpan durations = nullableDurations ?? TimeSpan.Zero;
            if (durations.TotalMinutes < 0)
            {
                // Jika durasinya kurang dari nol, maka tambahkan 1 hari ke durasinya
                durations = TimeSpan.FromDays(1) + durations;
            }
            int duration_in_minutes = (int)durations.TotalMinutes;

            //Validasi durasi, jika lebih dari 1 jam, kirim pesan error ke user
            if (duration_in_minutes > 60)
            {
                ModelState.AddModelError("dt_finish", "Downtime duration should not exceed 1 hour.");
                TempData["Message_dt"] = "Error: Downtime duration tidak boleh lebih dari 1 jam.";
            }

            //validasi lainnya
            if (ModelState.IsValid)
            {
                model.duration_minutes = duration_in_minutes;
                ssc_db.PIM_SSC_O_DOWNTIME.Add(model);
                ssc_db.SaveChanges();
                TempData["Message_dt"] = "Downtime Data Insert Successfully!";
            }
            var totalDt = ssc_db.PIM_SSC_O_DOWNTIME.Where(x => x.main_id_dt == model.main_id_dt && (x.downtime_code != "PD9" && x.downtime_code != "Z02")).Sum(x => x.duration_minutes);
            var totalNr = ssc_db.PIM_SSC_O_DOWNTIME.Where(x => x.main_id_dt == model.main_id_dt && (x.downtime_code == "PD9" || x.downtime_code == "Z02")).Sum(x => x.duration_minutes);

            // update data ke table PIM_USER_OUTPUT
            var data_main = ssc_db.PIM_SSC_MAIN.Where(x => x.main_id == model.main_id_dt).FirstOrDefault();
            if (data_main != null)
            {
                data_calc.totalDT = totalDt;
                data_calc.totalNotRun = totalNr;
                ssc_db.SaveChanges();

            }


            // DECLARE VARIABLE ACTUAL OUTPUT PLAN PER HOUR
            // DECLARE VARIABLE ACTUAL OUTPUT PLAN PER HOUR
            int actual_plan1 = ssc_db.PIM_SSC_OUTPUT.FirstOrDefault(x => x.main_id_o == data_main.main_id && x.hour_counter == 1)?.actoutput ?? 0;
            int actual_plan2 = ssc_db.PIM_SSC_OUTPUT.FirstOrDefault(x => x.main_id_o == data_main.main_id && x.hour_counter == 2)?.actoutput ?? 0;
            int actual_plan3 = ssc_db.PIM_SSC_OUTPUT.FirstOrDefault(x => x.main_id_o == data_main.main_id && x.hour_counter == 3)?.actoutput ?? 0;
            int actual_plan4 = ssc_db.PIM_SSC_OUTPUT.FirstOrDefault(x => x.main_id_o == data_main.main_id && x.hour_counter == 4)?.actoutput ?? 0;
            int actual_plan5 = ssc_db.PIM_SSC_OUTPUT.FirstOrDefault(x => x.main_id_o == data_main.main_id && x.hour_counter == 5)?.actoutput ?? 0;
            int actual_plan6 = ssc_db.PIM_SSC_OUTPUT.FirstOrDefault(x => x.main_id_o == data_main.main_id && x.hour_counter == 6)?.actoutput ?? 0;
            int actual_plan7 = ssc_db.PIM_SSC_OUTPUT.FirstOrDefault(x => x.main_id_o == data_main.main_id && x.hour_counter == 7)?.actoutput ?? 0;
            int actual_plan8 = ssc_db.PIM_SSC_OUTPUT.FirstOrDefault(x => x.main_id_o == data_main.main_id && x.hour_counter == 8)?.actoutput ?? 0;
            int actual_plan9 = ssc_db.PIM_SSC_OUTPUT.FirstOrDefault(x => x.main_id_o == data_main.main_id && x.hour_counter == 9)?.actoutput ?? 0;


            // DECLARE VARIABLE DOWNTIME PER HOUR
            var current_dt_1 = ssc_db.PIM_SSC_O_DOWNTIME.Where(x => x.main_id_dt == data_main.main_id && x.hour_counter == 1).ToList();
            var current_dt_2 = ssc_db.PIM_SSC_O_DOWNTIME.Where(x => x.main_id_dt == data_main.main_id && x.hour_counter == 2).ToList();
            var current_dt_3 = ssc_db.PIM_SSC_O_DOWNTIME.Where(x => x.main_id_dt == data_main.main_id && x.hour_counter == 3).ToList();
            var current_dt_4 = ssc_db.PIM_SSC_O_DOWNTIME.Where(x => x.main_id_dt == data_main.main_id && x.hour_counter == 4).ToList();
            var current_dt_5 = ssc_db.PIM_SSC_O_DOWNTIME.Where(x => x.main_id_dt == data_main.main_id && x.hour_counter == 5).ToList();
            var current_dt_6 = ssc_db.PIM_SSC_O_DOWNTIME.Where(x => x.main_id_dt == data_main.main_id && x.hour_counter == 6).ToList();
            var current_dt_7 = ssc_db.PIM_SSC_O_DOWNTIME.Where(x => x.main_id_dt == data_main.main_id && x.hour_counter == 7).ToList();
            var current_dt_8 = ssc_db.PIM_SSC_O_DOWNTIME.Where(x => x.main_id_dt == data_main.main_id && x.hour_counter == 8).ToList();
            var current_dt_9 = ssc_db.PIM_SSC_O_DOWNTIME.Where(x => x.main_id_dt == data_main.main_id && x.hour_counter == 9).ToList();

            // Check if there is downtime 
            if (data_main.shift_ != 0)
            {
                int totaltime1 = 0;
                if (data_main.shift_ == 2)
                {
                    if (current_dt_1.Any())
                    {
                        totaltime1 = 50;
                    }
                    else if (actual_plan1 != 0)
                    {
                        totaltime1 = 50;
                    }
                    else
                    {
                        totaltime1 = 0;
                    }
                }
                else
                {
                    if (current_dt_1.Any())
                    {
                        totaltime1 = 20;
                    }
                    else if (actual_plan1 != 0)
                    {
                        totaltime1 = 20;
                    }
                    else
                    {
                        totaltime1 = 0;
                    }

                }

                int totaltime2 = 0;
                if (current_dt_2.Any())
                {
                    totaltime2 = 60;
                }
                else if (actual_plan2 != 0)
                {
                    totaltime2 = 60;
                }
                else
                {
                    totaltime2 = 0;
                }

                int totaltime3 = 0;
                if (current_dt_3.Any())
                {
                    totaltime3 = 60;
                }
                else if (actual_plan3 != 0)
                {
                    totaltime3 = 60;
                }
                else
                {
                    totaltime3 = 0;
                }

                int totaltime4 = 0;
                if (current_dt_4.Any())
                {
                    totaltime4 = 60;
                }
                else if (actual_plan4 != 0)
                {
                    totaltime4 = 60;
                }
                else
                {
                    totaltime4 = 0;
                }

                int totaltime5 = 0;
                if (current_dt_5.Any())
                {
                    totaltime5 = 60;
                }
                else if (actual_plan5 != 0)
                {
                    totaltime5 = 60;
                }
                else
                {
                    totaltime5 = 0;
                }

                int totaltime6 = 0;
                if (current_dt_6.Any())
                {
                    totaltime6 = 60;
                }
                else if (actual_plan6 != 0)
                {
                    totaltime6 = 60;
                }
                else
                {
                    totaltime6 = 0;
                }

                int totaltime7 = 0;
                if (current_dt_7.Any())
                {
                    totaltime7 = 60;
                }
                else if (actual_plan7 != 0)
                {
                    totaltime7 = 60;
                }
                else
                {
                    totaltime7 = 0;
                }

                int totaltime8 = 0;
                if (current_dt_8.Any())
                {
                    if (data_main.shift_ == 3)
                    {
                        totaltime8 = 40;
                    }
                    else
                    {
                        totaltime8 = 60;
                    }

                }
                else if (actual_plan8 != 0)
                {
                    if (data_main.shift_ == 3)
                    {
                        totaltime8 = 40;
                    }
                    else
                    {
                        totaltime8 = 60;
                    }
                }
                else
                {
                    totaltime8 = 0;
                }

                if (data_main.shift_ == 3)
                {
                    data_calc.total_time = totaltime1 + totaltime2 + totaltime3 + totaltime4 + totaltime5 + totaltime6 + totaltime7 + totaltime8;
                    ssc_db.SaveChanges();
                    UpdatePnOperatingTime();
                    //UpdatePnStandardOutput();
                }
                else
                {
                    int totaltime9 = 0;
                    if (data_main.shift_ == 2)
                    {
                        if (current_dt_9.Any())
                        {
                            totaltime9 = 40;
                        }
                        else if (actual_plan9 != 0)
                        {
                            totaltime9 = 40;
                        }
                        else
                        {
                            totaltime9 = 0;
                        }

                    }
                    else
                    {
                        // shift 1
                        if (current_dt_9.Any())
                        {
                            totaltime9 = 70;
                        }
                        else if (actual_plan9 != 0)
                        {
                            totaltime9 = 70;
                        }
                        else
                        {
                            totaltime9 = 0;
                        }
                    }

                    data_calc.total_time = totaltime1 + totaltime2 + totaltime3 + totaltime4 + totaltime5 + totaltime6 + totaltime7 + totaltime8 + totaltime9;
                    ssc_db.SaveChanges();
                    UpdatePnOperatingTime();

                    //UpdatePnStandardOutput();
                }

            }


            // UPDATE STD OUTPUT
            if (data_calc.pn_operatingtime != null)
            {
                //UpdatePnStandardOutput();
                data_calc.pn_stdoutput = (3600 / data_main.pn_ct) * data_main.pn_cav * (data_calc.pn_operatingtime / 60);
                ssc_db.SaveChanges();
            }


            // redirect ke halaman ... dengan ID data yang baru ditambahkan
            return RedirectToAction("innerDataMC", new { main_id = model.main_id_dt });
        }



        // Delete Data
        public ActionResult downtimeDelete(int id)
        {
            var data_dt = ssc_db.PIM_SSC_O_DOWNTIME.Where(x => x.downtime_id == id).FirstOrDefault();
            var main_id = data_dt.main_id_dt;

            ssc_db.PIM_SSC_O_DOWNTIME.Remove(data_dt);
            ssc_db.SaveChanges();
            ViewBag.Messsage = "Record Delete Successfully";

            // data main user 
            var data_main = ssc_db.PIM_SSC_MAIN.Where(x => x.main_id == main_id).FirstOrDefault();
            // data output user 
            var data_output = ssc_db.PIM_SSC_OUTPUT.Where(x => x.main_id_o == main_id).FirstOrDefault();
            // data kalkulasi user 
            var data_calc = ssc_db.PIM_SSC_CALCULATION.Where(x => x.main_id_tc == main_id).FirstOrDefault();

            // Menghitung total downtime untuk setiap id lalu simpan ke pim_user_output
            var totalDt = ssc_db.PIM_SSC_O_DOWNTIME.Where(x => x.main_id_dt == main_id && (x.downtime_code != "PD9" && x.downtime_code != "Z02")).Sum(x => x.duration_minutes);
            // Menghitung total not running untuk setiap id
            var totalNr = ssc_db.PIM_SSC_O_DOWNTIME.Where(x => x.main_id_dt == main_id && (x.downtime_code == "PD9" || x.downtime_code == "Z02")).Sum(x => x.duration_minutes);

            if (data_calc != null)
            {
                data_calc.totalDT = totalDt;
                data_calc.totalNotRun = totalNr;
                ssc_db.SaveChanges();
            }


            // Check if there is downtime or output between shift 1, 2, 3
            // DECLARE VARIABLE ACTUAL PLAN
            int actual_plan1 = ssc_db.PIM_SSC_OUTPUT.FirstOrDefault(x => x.main_id_o == main_id && x.hour_counter == 1)?.actoutput ?? 0;
            int actual_plan2 = ssc_db.PIM_SSC_OUTPUT.FirstOrDefault(x => x.main_id_o == main_id && x.hour_counter == 2)?.actoutput ?? 0;
            int actual_plan3 = ssc_db.PIM_SSC_OUTPUT.FirstOrDefault(x => x.main_id_o == main_id && x.hour_counter == 3)?.actoutput ?? 0;
            int actual_plan4 = ssc_db.PIM_SSC_OUTPUT.FirstOrDefault(x => x.main_id_o == main_id && x.hour_counter == 4)?.actoutput ?? 0;
            int actual_plan5 = ssc_db.PIM_SSC_OUTPUT.FirstOrDefault(x => x.main_id_o == main_id && x.hour_counter == 5)?.actoutput ?? 0;
            int actual_plan6 = ssc_db.PIM_SSC_OUTPUT.FirstOrDefault(x => x.main_id_o == main_id && x.hour_counter == 6)?.actoutput ?? 0;
            int actual_plan7 = ssc_db.PIM_SSC_OUTPUT.FirstOrDefault(x => x.main_id_o == main_id && x.hour_counter == 7)?.actoutput ?? 0;
            int actual_plan8 = ssc_db.PIM_SSC_OUTPUT.FirstOrDefault(x => x.main_id_o == main_id && x.hour_counter == 8)?.actoutput ?? 0;
            int actual_plan9 = ssc_db.PIM_SSC_OUTPUT.FirstOrDefault(x => x.main_id_o == main_id && x.hour_counter == 9)?.actoutput ?? 0;

            // Check if there is downtime between 7:10 - 8:00
            if (data_main.shift_ == 1)
            {
                int totaltime1 = 0;
                if (ssc_db.PIM_SSC_O_DOWNTIME.Any(d => d.main_id_dt == main_id && d.dt_start >= new TimeSpan(22, 39, 0) && d.dt_finish < new TimeSpan(23, 01, 0)))
                {
                    totaltime1 = 20;
                }
                else if (actual_plan1 != 0)
                {
                    totaltime1 = 20;
                }
                else
                {
                    totaltime1 = 0;
                }

                //JAM 12 MALAM!
                int totaltime2 = 0;
                if (ssc_db.PIM_SSC_O_DOWNTIME.Any(d => d.main_id_dt == main_id && d.dt_start >= new TimeSpan(22, 59, 0) && d.dt_finish <= new TimeSpan(23, 59, 59)))
                {
                    totaltime2 = 60;
                }
                else if (actual_plan2 != 0)
                {
                    totaltime2 = 60;
                }
                else
                {
                    totaltime2 = 0;
                }

                int totaltime3 = 0;
                if (ssc_db.PIM_SSC_O_DOWNTIME.Any(d => d.main_id_dt == main_id && d.dt_start >= new TimeSpan(0, 1, 0) && d.dt_finish < new TimeSpan(1, 01, 0)))
                {
                    totaltime3 = 60;
                }
                else if (actual_plan3 != 0)
                {
                    totaltime3 = 60;
                }
                else
                {
                    totaltime3 = 0;
                }

                int totaltime4 = 0;
                if (ssc_db.PIM_SSC_O_DOWNTIME.Any(d => d.main_id_dt == main_id && d.dt_start >= new TimeSpan(0, 59, 0) && d.dt_finish < new TimeSpan(2, 01, 0)))
                {
                    totaltime4 = 60;
                }
                else if (actual_plan4 != 0)
                {
                    totaltime4 = 60;
                }
                else
                {
                    totaltime4 = 0;
                }

                int totaltime5 = 0;
                if (ssc_db.PIM_SSC_O_DOWNTIME.Any(d => d.main_id_dt == main_id && d.dt_start >= new TimeSpan(1, 59, 0) && d.dt_finish < new TimeSpan(3, 01, 0)))
                {
                    totaltime5 = 60;
                }
                else if (actual_plan5 != 0)
                {
                    totaltime5 = 60;
                }
                else
                {
                    totaltime5 = 0;
                }

                int totaltime6 = 0;
                if (ssc_db.PIM_SSC_O_DOWNTIME.Any(d => d.main_id_dt == main_id && d.dt_start >= new TimeSpan(2, 59, 0) && d.dt_finish < new TimeSpan(4, 01, 0)))
                {
                    totaltime6 = 60;
                }
                else if (actual_plan6 != 0)
                {
                    totaltime6 = 60;
                }
                else
                {
                    totaltime6 = 0;
                }

                int totaltime7 = 0;
                if (ssc_db.PIM_SSC_O_DOWNTIME.Any(d => d.main_id_dt == main_id && d.dt_start >= new TimeSpan(3, 59, 0) && d.dt_finish < new TimeSpan(5, 01, 0)))
                {
                    totaltime7 = 60;
                }
                else if (actual_plan7 != 0)
                {
                    totaltime7 = 60;
                }
                else
                {
                    totaltime7 = 0;
                }


                int totaltime8 = 0;
                if (ssc_db.PIM_SSC_O_DOWNTIME.Any(d => d.main_id_dt == main_id && d.dt_start >= new TimeSpan(4, 59, 0) && d.dt_finish < new TimeSpan(6, 01, 0)))
                {
                    totaltime8 = 60;
                }
                else if (actual_plan8 != 0)
                {
                    totaltime8 = 60;
                }
                else
                {
                    totaltime8 = 0;
                }

                int totaltime9 = 0;
                if (ssc_db.PIM_SSC_O_DOWNTIME.Any(d => d.main_id_dt == main_id && d.dt_start >= new TimeSpan(5, 59, 0) && d.dt_finish < new TimeSpan(7, 11, 0)))
                {
                    totaltime9 = 70;
                }
                else if (actual_plan9 != 0)
                {
                    totaltime9 = 70;
                }
                else
                {
                    totaltime9 = 0;
                }

                data_calc.total_time = totaltime1 + totaltime2 + totaltime3 + totaltime4 + totaltime5 + totaltime6 + totaltime7 + totaltime8 + totaltime9;
                //data3.total_time = 510;
                ssc_db.SaveChanges();
                UpdatePnOperatingTime();

                //UpdatePnStandardOutput();

            }
            else if (data_main.shift_ == 2)
            {
                int totaltime1 = 0;
                if (ssc_db.PIM_SSC_O_DOWNTIME.Any(d => d.main_id_dt == main_id && d.dt_start >= new TimeSpan(6, 59, 0) && d.dt_finish < new TimeSpan(8, 01, 0)))
                {
                    totaltime1 = 50;
                }
                else if (actual_plan1 != 0)
                {
                    totaltime1 = 50;
                }
                else
                {
                    totaltime1 = 0;
                }

                int totaltime2 = 0;
                if (ssc_db.PIM_SSC_O_DOWNTIME.Any(d => d.main_id_dt == main_id && d.dt_start >= new TimeSpan(7, 59, 0) && d.dt_finish < new TimeSpan(9, 01, 0)))
                {
                    totaltime2 = 60;
                }
                else if (actual_plan2 != 0)
                {
                    totaltime2 = 60;
                }
                else
                {
                    totaltime2 = 0;
                }

                int totaltime3 = 0;
                if (ssc_db.PIM_SSC_O_DOWNTIME.Any(d => d.main_id_dt == main_id && d.dt_start >= new TimeSpan(8, 59, 0) && d.dt_finish < new TimeSpan(10, 01, 0)))
                {
                    totaltime3 = 60;
                }
                else if (actual_plan3 != 0)
                {
                    totaltime3 = 60;
                }
                else
                {
                    totaltime3 = 0;
                }

                int totaltime4 = 0;
                if (ssc_db.PIM_SSC_O_DOWNTIME.Any(d => d.main_id_dt == main_id && d.dt_start >= new TimeSpan(9, 59, 0) && d.dt_finish < new TimeSpan(11, 01, 0)))
                {
                    totaltime4 = 60;
                }
                else if (actual_plan4 != 0)
                {
                    totaltime4 = 60;
                }
                else
                {
                    totaltime4 = 0;
                }

                int totaltime5 = 0;
                if (ssc_db.PIM_SSC_O_DOWNTIME.Any(d => d.main_id_dt == main_id && d.dt_start >= new TimeSpan(10, 59, 0) && d.dt_finish < new TimeSpan(12, 01, 0)))
                {
                    totaltime5 = 60;
                }
                else if (actual_plan5 != 0)
                {
                    totaltime5 = 60;
                }
                else
                {
                    totaltime5 = 0;
                }

                int totaltime6 = 0;
                if (ssc_db.PIM_SSC_O_DOWNTIME.Any(d => d.main_id_dt == main_id && d.dt_start >= new TimeSpan(11, 59, 0) && d.dt_finish < new TimeSpan(13, 01, 0)))
                {
                    totaltime6 = 60;
                }
                else if (actual_plan6 != 0)
                {
                    totaltime6 = 60;
                }
                else
                {
                    totaltime6 = 0;
                }

                int totaltime7 = 0;
                if (ssc_db.PIM_SSC_O_DOWNTIME.Any(d => d.main_id_dt == main_id && d.dt_start >= new TimeSpan(12, 59, 0) && d.dt_finish < new TimeSpan(14, 01, 0)))
                {
                    totaltime7 = 60;
                }
                else if (actual_plan7 != 0)
                {
                    totaltime7 = 60;
                }
                else
                {
                    totaltime7 = 0;
                }

                int totaltime8 = 0;
                if (ssc_db.PIM_SSC_O_DOWNTIME.Any(d => d.main_id_dt == main_id && d.dt_start >= new TimeSpan(13, 59, 0) && d.dt_finish < new TimeSpan(15, 01, 0)))
                {
                    totaltime8 = 60;
                }
                else if (actual_plan8 != 0)
                {
                    totaltime8 = 60;
                }
                else
                {
                    totaltime8 = 0;
                }

                int totaltime9 = 0;
                if (ssc_db.PIM_SSC_O_DOWNTIME.Any(d => d.main_id_dt == main_id && d.dt_start >= new TimeSpan(14, 59, 0) && d.dt_finish < new TimeSpan(15, 41, 0)))
                {
                    totaltime9 = 40;
                }
                else if (actual_plan9 != 0)
                {
                    totaltime9 = 40;
                }
                else
                {
                    totaltime9 = 0;
                }
                data_calc.total_time = totaltime1 + totaltime2 + totaltime3 + totaltime4 + totaltime5 + totaltime6 + totaltime7 + totaltime8 + totaltime9;
                ssc_db.SaveChanges();
                UpdatePnOperatingTime();

                //UpdatePnStandardOutput();
            }
            else if (data_main.shift_ == 3)
            {

                int totaltime1 = 0;
                if (ssc_db.PIM_SSC_O_DOWNTIME.Any(d => d.main_id_dt == main_id && d.dt_start >= new TimeSpan(15, 39, 0) && d.dt_finish < new TimeSpan(16, 01, 0)))
                {
                    totaltime1 = 20;
                }
                else if (actual_plan1 != 0)
                {
                    totaltime1 = 20;
                }
                else
                {
                    totaltime1 = 0;
                }

                int totaltime2 = 0;
                if (ssc_db.PIM_SSC_O_DOWNTIME.Any(d => d.main_id_dt == main_id && d.dt_start >= new TimeSpan(15, 59, 0) && d.dt_finish < new TimeSpan(17, 01, 0)))
                {
                    totaltime2 = 60;
                }
                else if (actual_plan2 != 0)
                {
                    totaltime2 = 60;
                }
                else
                {
                    totaltime2 = 0;
                }

                int totaltime3 = 0;
                if (ssc_db.PIM_SSC_O_DOWNTIME.Any(d => d.main_id_dt == main_id && d.dt_start >= new TimeSpan(16, 59, 0) && d.dt_finish < new TimeSpan(18, 01, 0)))
                {
                    totaltime3 = 60;
                }
                else if (actual_plan3 != 0)
                {
                    totaltime3 = 60;
                }
                else
                {
                    totaltime3 = 0;
                }

                int totaltime4 = 0;
                if (ssc_db.PIM_SSC_O_DOWNTIME.Any(d => d.main_id_dt == main_id && d.dt_start >= new TimeSpan(17, 59, 0) && d.dt_finish < new TimeSpan(19, 01, 0)))
                {
                    totaltime4 = 60;
                }
                else if (actual_plan4 != 0)
                {
                    totaltime4 = 60;
                }
                else
                {
                    totaltime4 = 0;
                }

                int totaltime5 = 0;
                if (ssc_db.PIM_SSC_O_DOWNTIME.Any(d => d.main_id_dt == main_id && d.dt_start >= new TimeSpan(18, 59, 0) && d.dt_finish < new TimeSpan(20, 01, 0)))
                {
                    totaltime5 = 60;
                }
                else if (actual_plan5 != 0)
                {
                    totaltime5 = 60;
                }
                else
                {
                    totaltime5 = 0;
                }

                int totaltime6 = 0;
                if (ssc_db.PIM_SSC_O_DOWNTIME.Any(d => d.main_id_dt == main_id && d.dt_start >= new TimeSpan(19, 59, 0) && d.dt_finish < new TimeSpan(21, 01, 0)))
                {
                    totaltime6 = 60;
                }
                else if (actual_plan6 != 0)
                {
                    totaltime6 = 60;
                }
                else
                {
                    totaltime6 = 0;
                }

                int totaltime7 = 0;
                if (ssc_db.PIM_SSC_O_DOWNTIME.Any(d => d.main_id_dt == main_id && d.dt_start >= new TimeSpan(20, 59, 0) && d.dt_finish < new TimeSpan(22, 01, 0)))
                {
                    totaltime7 = 60;
                }
                else if (actual_plan7 != 0)
                {
                    totaltime7 = 60;
                }
                else
                {
                    totaltime7 = 0;
                }

                int totaltime8 = 0;
                if (ssc_db.PIM_SSC_O_DOWNTIME.Any(d => d.main_id_dt == main_id && d.dt_start >= new TimeSpan(21, 59, 0) && d.dt_finish < new TimeSpan(22, 41, 0)))
                {
                    totaltime8 = 40;
                }
                else if (actual_plan8 != 0)
                {
                    totaltime8 = 40;
                }
                else
                {
                    totaltime8 = 0;
                }

                data_calc.total_time = totaltime1 + totaltime2 + totaltime3 + totaltime4 + totaltime5 + totaltime6 + totaltime7 + totaltime8;
                ssc_db.SaveChanges();
                UpdatePnOperatingTime();

                //UpdatePnStandardOutput();
            }


            // UPDATE STD OUTPUT
            if (data_calc.pn_operatingtime != null)
            {
                data_calc.pn_stdoutput = (3600 / data_main.pn_ct) * data_main.pn_cav * (data_calc.pn_operatingtime / 60);
                ssc_db.SaveChanges();
            }

            return RedirectToAction("innerDataMC", new { main_id = main_id });
        }

        // ---------------------------------------------------------------------------------------------------------- //


        // Add New Data REJECT from user to database table pim_user_output
        [HttpGet]
        public ActionResult outputReject()
        {
            return View();
        }
        [HttpPost]
        public ActionResult outputReject(PIM_SSC_O_REJECT model)
        {
            // Insert data
            ssc_db.PIM_SSC_O_REJECT.Add(model);
            ssc_db.SaveChanges();

            // Update user output data
            var data_calc = ssc_db.PIM_SSC_CALCULATION.FirstOrDefault(x => x.main_id_tc == model.main_id_rj);

            if (data_calc != null)
            {
                // Calculate total rj_quantity for the user
                var totalRj = ssc_db.PIM_SSC_O_REJECT.Where(x => x.main_id_rj == model.main_id_rj).Sum(x => x.reject_qty);
                // Update totalRj and actual_good_output for the user
                data_calc.totalRJ = totalRj;
                ssc_db.SaveChanges();

                data_calc.total_good_output = (data_calc.total_output_pcs ?? 0) - (data_calc.totalRJ ?? 0);
                ssc_db.SaveChanges();
            }

            // Redirect to innerDataMC with the updated main_id_rj
            return RedirectToAction("innerDataMC", new { main_id = model.main_id_rj });
        }

        // Delete Data REJECT
        public ActionResult rejectDelete(int reject_id)
        {
            var data_reject = ssc_db.PIM_SSC_O_REJECT.Where(x => x.reject_id == reject_id).FirstOrDefault();

            var main_id_rj = data_reject.main_id_rj;

            ssc_db.PIM_SSC_O_REJECT.Remove(data_reject);
            ssc_db.SaveChanges();
            ViewBag.Messsage = "Record Delete Successfully";

            // data kalkulasi total
            var data_calc = ssc_db.PIM_SSC_CALCULATION.Where(x => x.main_id_tc == main_id_rj).FirstOrDefault();

            // hitung total quantity untuk setiap id main
            var totalRj = ssc_db.PIM_SSC_O_REJECT.Where(x => x.main_id_rj == main_id_rj).Sum(x => x.reject_qty);

            // hitung jumlah good output
            data_calc.total_good_output = (data_calc.total_output_pcs ?? 0) - totalRj;
            ssc_db.SaveChanges();

            if (data_calc != null)
            {
                data_calc.totalRJ = totalRj;
                // Masukkan Good Output di mana total actual_output dikurang total rj_quantity // 
                data_calc.total_good_output = (data_calc.total_output_pcs ?? 0) - (totalRj ?? 0);
                ssc_db.SaveChanges();
            }

            return RedirectToAction("innerDataMC", new { main_id = main_id_rj });
        }


        public ActionResult insertSchedule()
        {
            // Session
            int? inputShift = System.Web.HttpContext.Current.Session["myShift"] as int?;
            DateTime? dateFilter = System.Web.HttpContext.Current.Session["myDate"] as DateTime?;

            // Default date
            DateTime now = DateTime.Now;
            DateTime filterDate = dateFilter ?? now.Date;

            // Check if inputShift is null or 0
            if (!inputShift.HasValue || inputShift.Value == 0)
            {
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

            // Assign values to ViewBag
            ViewBag.inputShift = inputShift.Value;
            ViewBag.FilterDate = filterDate;

            // ---------------------------------------------- //

            if (inputShift == 0 || filterDate == null)
            {
                // Handle missing session variables here
                // For example, you can redirect to an error page or display an error message
                return RedirectToAction("error404", "Home");
            }

            // Ambil data dari database Namlos table East_Schedule
            List<East_Schedule> East_Schedule_Data = East_Schedule.GetEast_ScheduleData();

            if (inputShift == 1)
            {
                East_Schedule_Data = East_Schedule_Data.Where(x => x.date == filterDate && x.sh1 != 0).ToList();
                ViewBag.shift_ = 1;
            }
            else if (inputShift == 2)
            {
                East_Schedule_Data = East_Schedule_Data.Where(x => x.date == filterDate && x.sh2 != 0).ToList();
                ViewBag.shift_ = 2;
            }
            else if (inputShift == 3)
            {
                East_Schedule_Data = East_Schedule_Data.Where(x => x.date == filterDate && x.sh3 != 0).ToList();
                ViewBag.shift_ = 3;
            }

            // mengirimkan list nomor mesin sebagai model view
            return View(East_Schedule_Data);
        }


        [HttpPost]
        public ActionResult InsertSchedule([Bind(Include = "mc,part,sh1,sh2,sh3")] List<East_Schedule> models)
        {
            // session
            int? inputShift = System.Web.HttpContext.Current.Session["myShift"] as int?;
            DateTime? filterDate = System.Web.HttpContext.Current.Session["myDate"] as DateTime?;

            // Default date
            DateTime now = DateTime.Now;
            DateTime dateFilter = filterDate ?? now.Date;

            // Check if inputShift is null or 0
            if (!inputShift.HasValue || inputShift.Value == 0)
            {
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
                    dateFilter = now.AddDays(1).Date;
                }
            }

            // Ambil data dari database Namlos table East_Schedule
            List<East_Schedule> eastScheduleData = East_Schedule.GetEast_ScheduleData();
            var dataSchedule = eastScheduleData.Where(x => x.date == dateFilter && x.mc != null).ToList();

            // data tujuan 
            DateTime dataDate1 = dateFilter;
            DateTime weekEnding1 = dataDate1.AddDays(6 - (int)dataDate1.DayOfWeek);

            // Convert the weekEnding DateTime object to the desired format
            string formattedWeekEnding = weekEnding1.ToString("yyyy-MM-dd");

            // Insert multiple data to database
            foreach (var model in models)
            {
                // Cek apakah data sudah ada di dalam database
                bool isDataExists = ssc_db.PIM_SSC_MAIN.Any(x => x.date_ == dateFilter && x.shift_ == inputShift && x.no_machine == model.mc && x.partnumber == model.part);
                if (!isDataExists)
                {
                    PIM_SSC_MAIN pim_ssc_main = new PIM_SSC_MAIN
                    {
                        date_ = dateFilter,
                        shift_ = inputShift,
                        we_ = formattedWeekEnding,
                        no_machine = model.mc,
                        partnumber = model.part,
                        mc_area = GetMcArea(model.mc)
                    };

                    if (inputShift == 1)
                    {
                        pim_ssc_main.sch = model.sh1;
                    }
                    else if (inputShift == 2)
                    {
                        pim_ssc_main.sch = model.sh2;
                    }
                    else if (inputShift == 3)
                    {
                        pim_ssc_main.sch = model.sh3;
                    }
                    else
                    {
                        pim_ssc_main.sch = 0;
                    }

                    ssc_db.PIM_SSC_MAIN.Add(pim_ssc_main);
                    ssc_db.SaveChanges();
                }
            }

            var newData = ssc_db.PIM_SSC_MAIN.Where(x => x.shift_ == inputShift && x.date_ == dateFilter).OrderBy(x => x.no_machine).ToList();

            // jika ada data yang ditemukan, tampilan kembali ke firstPage dengan shift dan date yang sudah diinput pertama kali menggunakan Session

            if (newData.Any())
            {
                return RedirectToAction("firstPage", new { shiftEmp = inputShift, dateFilter = dateFilter });
            }
            else
            {
                return View("error404", "Home");
            }
        }


        // Input area based on mc
        private int GetMcArea(string machineCode)
        {
            if (machineCode.StartsWith("1F-A") || machineCode.StartsWith("1F-B") || machineCode.StartsWith("1F-C") || machineCode.StartsWith("1F-D") || machineCode.StartsWith("C") || machineCode.StartsWith("M3"))
            {
                return 1;
            }
            else if (machineCode.StartsWith("1F-E") || machineCode.StartsWith("1F-F") || machineCode.StartsWith("1F-G") || machineCode.StartsWith("1F-H"))
            {
                return 2;
            }
            else if (machineCode.StartsWith("1F-J") || machineCode.StartsWith("1F-K") || machineCode.StartsWith("1F-L"))
            {
                return 3;
            }
            else if (machineCode.StartsWith("1F-M") || machineCode.StartsWith("1F-N") || machineCode.StartsWith("1F-P"))
            {
                return 4;
            }
            else if (machineCode.StartsWith("GF-A") || machineCode.StartsWith("GF-B") || machineCode.StartsWith("GF-C") || machineCode.StartsWith("GF-D") || machineCode.StartsWith("GF-E") || machineCode.StartsWith("GF-F"))
            {
                return 5;
            }
            else if (machineCode.StartsWith("GF-G") || machineCode.StartsWith("GF-H") || machineCode.StartsWith("GF-J") || machineCode.StartsWith("GF-K") || machineCode.StartsWith("GF-L") || machineCode.StartsWith("GF-M"))
            {
                return 6;
            }

            // Jika tidak ada kondisi yang cocok, maka mengembalikan nilai default
            return 0;
        }



        // -----------------------------------KPK DATA IMPORT FROM EXCEL-------------------------------------- //

        public ActionResult importDataKPK()
        {
            var data_kpk = ssc_db.pim_user_kpk.Where(x => x.op_kpk != null && x.op_name != null).OrderBy(x => x.op_name).ThenBy(x => x.op_kpk).ToList();

            // mengirimkan list nomor mesin sebagai model view
            return View(data_kpk);
        }

        // POST: Import KPK Data
        [HttpPost]
        public ActionResult importDataKPK(HttpPostedFileBase file)
        {
            DateTime myDate = DateTime.Now;

            int myShift = 0;
            if (myShift == 0)
            {
                if (myDate.TimeOfDay >= new TimeSpan(7, 10, 0) && myDate.TimeOfDay < new TimeSpan(15, 40, 0))
                {
                    myShift = 2;
                }
                else if (myDate.TimeOfDay >= new TimeSpan(15, 40, 0) && myDate.TimeOfDay < new TimeSpan(22, 40, 0))
                {
                    myShift = 3;
                }
                else
                {
                    myShift = 1;
                    myDate = myDate.AddDays(1).Date;
                }
            }

            // Set LicenseContext
            ExcelPackage.LicenseContext = LicenseContext.Commercial;

            if (file != null && file.ContentLength > 0)
            {
                // get file extension
                var fileExtension = Path.GetExtension(file.FileName);

                // check if file is an Excel file
                if (fileExtension == ".xlsx")
                {
                    try
                    {
                        // create Excel package from uploaded file
                        using (var package = new ExcelPackage(file.InputStream))
                        {
                            // get the first worksheet in the Excel package
                            ExcelWorksheet worksheet = package.Workbook.Worksheets.First();

                            // initialize counter variable
                            int rowsImported = 0;

                            // loop through rows in the worksheet
                            for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
                            {
                                // create new pim_user_kpk object
                                pim_user_kpk pimUserKpk = new pim_user_kpk();

                                // set values for kpk, name
                                pimUserKpk.op_kpk = worksheet.Cells[row, 1].Text?.Trim();
                                pimUserKpk.op_name = worksheet.Cells[row, 2].Text?.Trim();

                                // Jika data valid dan tidak ada duplikat, maka masukkan ke dalam database
                                if (!string.IsNullOrEmpty(pimUserKpk.op_kpk) && !ssc_db.pim_user_kpk.Any(x => x.op_kpk == pimUserKpk.op_kpk))
                                {
                                    ssc_db.pim_user_kpk.Add(pimUserKpk);
                                    ssc_db.SaveChanges();
                                    rowsImported++;
                                }
                            }

                            // display success message with the number of imported rows
                            TempData["MessageImport"] = "Import successful: Data berjumlah " + rowsImported + " rows berhasil dimasukkan";

                            return RedirectToAction("importDataKPK");
                        }
                    }
                    catch (Exception ex)
                    {
                        ViewBag.Error = ex.Message;
                    }
                }
                else
                {
                    ViewBag.Error = "File must be an Excel file";
                }
            }
            else
            {
                ViewBag.Error = "No file selected";
            }
            return View();
        }

        // POST: Import KPK PART NUMBER
        [HttpPost]
        public ActionResult importDataPN(HttpPostedFileBase file)
        {
            DateTime myDate = DateTime.Now;

            // Set LicenseContext
            ExcelPackage.LicenseContext = LicenseContext.Commercial;

            if (file != null && file.ContentLength > 0)
            {
                // get file extension
                var fileExtension = Path.GetExtension(file.FileName);

                // check if file is an Excel file
                if (fileExtension == ".xlsx")
                {
                    try
                    {
                        // create Excel package from uploaded file
                        using (var package = new ExcelPackage(file.InputStream))
                        {
                            // get the first worksheet in the Excel package
                            ExcelWorksheet worksheet = package.Workbook.Worksheets.First();

                            // initialize counter variable
                            int rowsImported = 0;

                            // loop through rows in the worksheet
                            for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
                            {
                                // create new pim_user_kpk object
                                pim_part_number_list pim_pn = new pim_part_number_list();

                                // set values for kpk, name
                                // ID,PART, DESC, MOLD, FAMILY, CAVITY, CT, MCHRS, DATE_INPUT
                                pim_pn.PART = worksheet.Cells[row, 1].Text?.Trim();
                                // Default Mchrs
                                if (!string.IsNullOrEmpty(pim_pn.MOLD) && pim_pn.MOLD.Trim() != "#N/A")
                                {
                                    pim_pn.DESC = worksheet.Cells[row, 2].Text?.Trim();
                                    pim_pn.MOLD = worksheet.Cells[row, 6].Text?.Trim();


                                    // Attempt to convert to int using null coalescing operator
                                    pim_pn.CT = int.TryParse(worksheet.Cells[row, 7].Text?.Trim(), out int ctValue) ? ctValue : 0; // Default value or another suitable approach
                                    pim_pn.CAVITY = int.TryParse(worksheet.Cells[row, 8].Text?.Trim(), out int cavityValue) ? cavityValue : 0; // Default value or another suitable approach

                                    //pim_pn.MCHRS = float.TryParse(worksheet.Cells[row, 7].Text?.Trim(), out float mchrsValue) ? mchrsValue : 0; 
                                    //pim_pn.FAMILY = int.TryParse(worksheet.Cells[row, 4].Text?.Trim(), out int familyValue) ? familyValue: 0; 

                                    pim_pn.PN_COLORANT = worksheet.Cells[row, 13].Text?.Trim();
                                    pim_pn.COLOR = worksheet.Cells[row, 12].Text?.Trim();
                                    pim_pn.MATERIAL = worksheet.Cells[row, 4].Text?.Trim();

                                }
                                // Default Update, Today Date
                                pim_pn.DATE_INPUT = myDate;

                                // Cek apakah data dengan nilai PART yang sama sudah ada di database
                                // var existingPart = ssc_db.pim_part_number_list.FirstOrDefault(x => x.PART == pim_pn.PART);

                                //// Jika data sudah ada, hapus data lama dan tambahkan data terbaru
                                //if (existingPart != null)
                                //{
                                //    ssc_db.pim_part_number_list.Remove(existingPart); // Hapus data lama
                                //    ssc_db.pim_part_number_list.Add(pim_pn); // Tambahkan data terbaru
                                //    ssc_db.SaveChanges();
                                //    rowsImported++; // Atau sesuai kebutuhan 
                                //}
                                //// Jika data belum ada, tambahkan data baru
                                //else if (!string.IsNullOrEmpty(pim_pn.PART))
                                //{
                                //    ssc_db.pim_part_number_list.Add(pim_pn);
                                //    ssc_db.SaveChanges();
                                //    rowsImported++;
                                //}


                                //Jika data valid dan tidak ada duplikat, maka masukkan ke dalam database
                                if (!string.IsNullOrEmpty(pim_pn.PART) && !ssc_db.pim_part_number_list.Any(x => x.PART == pim_pn.PART))
                                {
                                    ssc_db.pim_part_number_list.Add(pim_pn);
                                    ssc_db.SaveChanges();
                                    rowsImported++;
                                }
                            }

                            // display success message with the number of imported rows
                            TempData["MessageImport"] = "Import successful: Data berjumlah " + rowsImported + " rows berhasil dimasukkan";

                            return RedirectToAction("importDataKPK");
                        }
                    }
                    catch (Exception ex)
                    {
                        ViewBag.Error = ex.Message;
                    }
                }
                else
                {
                    ViewBag.Error = "File must be an Excel file";
                }
            }
            else
            {
                ViewBag.Error = "No file part number selected";
            }
            return View();
        }


        // GET: KPI Performance
        public ActionResult KPI_PERFORMANCE(DateTime? currentDate = null)
        {
            DateTime now = DateTime.Now;
            DateTime filterDate = currentDate ?? now.Date;

            //if (currentShift == 0)
            //{
            //    if (now.TimeOfDay >= new TimeSpan(7, 10, 0) && now.TimeOfDay < new TimeSpan(15, 40, 0))
            //    {
            //        currentShift = 2;
            //    }
            //    else if (now.TimeOfDay >= new TimeSpan(15, 40, 0) && now.TimeOfDay < new TimeSpan(22, 40, 0))
            //    {
            //        currentShift = 3;
            //    }
            //    else
            //    {
            //        currentShift = 1;
            //        filterDate = now.AddDays(1).Date;
            //    }
            //}

            // week ending
            DateTime weekEnding = filterDate.AddDays(6 - (int)filterDate.DayOfWeek);
            // testing 
            // DateTime weekEnding = new DateTime(2023, 8, 26);


            // Ambil data 7 hari terakhir
            DateTime startDate = DateTime.Now.Date;
            DateTime endDate = DateTime.Now.Date;
            if (weekEnding != null)
            {
                startDate = weekEnding.AddDays(-6);
                endDate = weekEnding;
            }
            else
            {
                startDate = currentDate.HasValue ? currentDate.Value.AddDays(-6) : DateTime.Now.Date.AddDays(-6);
                endDate = currentDate ?? DateTime.Now.Date;
            }

            // Assign value to ViewBag
            //ViewBag.currentShift = currentShift;
            ViewBag.FilterDate = filterDate;
            ViewBag.startDate = startDate;
            ViewBag.endDate = endDate;
            ViewBag.weekEnding = weekEnding.ToShortDateString();

            // Display data KPI Performance
            //var data_kpi_shift = ssc_db.pim_kpi_performance
            //    .Where(x => x.DATE_ >= startDate && x.DATE_ <= endDate)
            //    .ToList();

            // LOGIC NYA AKAN SEPERTI INI JIKA RATA-RATA DATA KPI
            // 
            var data_kpi_day = ssc_db.pim_kpi_performance.Where(x => x.DATE_ >= startDate && x.DATE_ <= endDate).ToList();

            var groupedData = data_kpi_day
               .GroupBy(x => x.DATE_)
               .Select(group => new
               {
                   Date_ = group.Key,
                   AverageAudit5S = group.Average(x => x.AUDIT_5S),
                   AverageCompliance = group.Average(x => x.COMPLIANCE),
                   AverageEHS_PATROL = group.Average(x => x.EHS_PATROL),
                   SumLBO_PO_PCS = group.Sum(x => x.LBO_PO_PCS),
                   AverageLBO_PO_PPM = group.Average(x => x.LBO_PO_PPM),
                   AverageEFF = group.Average(x => x.EFF),
                   AverageSCRAP = group.Average(x => x.SCRAP),
                   AverageDSA_ITEM = group.Average(x => x.DSA_ITEM),
                   AverageDSA_VOLUME = group.Average(x => x.DSA_VOLUME),
                   // ... Repeat for other KPIs ...
               })
               .OrderBy(x => x.Date_).ToList()
                .Select(x => new pim_kpi_performance
                {
                    DATE_ = x.Date_,
                    AUDIT_5S = x.AverageAudit5S,
                    COMPLIANCE = x.AverageCompliance,
                    EHS_PATROL = x.AverageEHS_PATROL,
                    LBO_PO_PCS = x.SumLBO_PO_PCS,
                    LBO_PO_PPM = x.AverageLBO_PO_PPM,
                    EFF = x.AverageEFF,
                    SCRAP = x.AverageSCRAP,
                    DSA_ITEM = x.AverageDSA_ITEM,
                    DSA_VOLUME = x.AverageDSA_VOLUME,

                }).ToList();


            Dictionary<DateTime, float> audit5SValues = new Dictionary<DateTime, float>();
            Dictionary<DateTime, float> complianceValues = new Dictionary<DateTime, float>();
            Dictionary<DateTime, float> ehs_patrolValues = new Dictionary<DateTime, float>();
            Dictionary<DateTime, float> LBO_PO_PCSValues = new Dictionary<DateTime, float>();
            Dictionary<DateTime, float> LBO_PO_PPMValues = new Dictionary<DateTime, float>();
            Dictionary<DateTime, float> EFFValues = new Dictionary<DateTime, float>();
            Dictionary<DateTime, float> SCRAPValues = new Dictionary<DateTime, float>();
            Dictionary<DateTime, float> DSA_ITEMValues = new Dictionary<DateTime, float>();
            Dictionary<DateTime, float> DSA_VOLUMEValues = new Dictionary<DateTime, float>();
            // ... Define other KPI dictionaries ...

            foreach (var data in groupedData)
            {
                if (data.AUDIT_5S != null)
                {
                    audit5SValues[data.DATE_.Value] = (float)data.AUDIT_5S.Value;
                }
                if (data.COMPLIANCE != null)
                {
                    complianceValues[data.DATE_.Value] = (float)data.COMPLIANCE.Value;
                }
                if (data.EHS_PATROL != null)
                {
                    ehs_patrolValues[data.DATE_.Value] = (float)data.EHS_PATROL.Value;
                }
                if (data.LBO_PO_PCS != null)
                {
                    LBO_PO_PCSValues[data.DATE_.Value] = (float)data.LBO_PO_PCS.Value;
                }
                if (data.LBO_PO_PPM != null)
                {
                    LBO_PO_PPMValues[data.DATE_.Value] = (float)data.LBO_PO_PPM.Value;
                }
                if (data.EFF != null)
                {
                    EFFValues[data.DATE_.Value] = (float)data.EFF.Value;
                }
                if (data.SCRAP != null)
                {
                    SCRAPValues[data.DATE_.Value] = (float)data.SCRAP.Value;
                }
                if (data.DSA_ITEM != null)
                {
                    DSA_ITEMValues[data.DATE_.Value] = (float)data.DSA_ITEM.Value;
                }
                if (data.DSA_VOLUME != null)
                {
                    DSA_VOLUMEValues[data.DATE_.Value] = (float)data.DSA_VOLUME.Value;
                }
                // ... Set values for other KPIs ...
            }


            // ======================================================================= //

            //Dictionary<DateTime, float> audit5SValues = new Dictionary<DateTime, float>();
            //Dictionary<DateTime, float> complianceValues = new Dictionary<DateTime, float>();
            //Dictionary<DateTime, float> ehs_patrolValues = new Dictionary<DateTime, float>();
            //Dictionary<DateTime, float> LBO_PO_PCSValues = new Dictionary<DateTime, float>();
            //Dictionary<DateTime, float> LBO_PO_PPMValues = new Dictionary<DateTime, float>();
            //Dictionary<DateTime, float> EFFValues = new Dictionary<DateTime, float>();
            //Dictionary<DateTime, float> SCRAPValues = new Dictionary<DateTime, float>();
            //Dictionary<DateTime, float> DSA_ITEMValues = new Dictionary<DateTime, float>();
            //Dictionary<DateTime, float> DSA_VOLUMEValues = new Dictionary<DateTime, float>();



            //foreach (var data in data_kpi_shift)
            //{
            //    if (data.AUDIT_5S != null)
            //    {
            //        audit5SValues[data.DATE_.Value] = (float)data.AUDIT_5S.Value;
            //    }
            //    if (data.COMPLIANCE != null)
            //    {
            //        complianceValues[data.DATE_.Value] = (float)data.COMPLIANCE.Value;
            //    }
            //    if (data.EHS_PATROL != null)
            //    {
            //        ehs_patrolValues[data.DATE_.Value] = (float)data.EHS_PATROL.Value;
            //    }
            //    if (data.LBO_PO_PCS != null)
            //    {
            //        LBO_PO_PCSValues[data.DATE_.Value] = (float)data.LBO_PO_PCS.Value;
            //    }
            //    if (data.LBO_PO_PPM != null)
            //    {
            //        LBO_PO_PPMValues[data.DATE_.Value] = (float)data.LBO_PO_PPM.Value;
            //    }
            //    if (data.EFF != null)
            //    {
            //        EFFValues[data.DATE_.Value] = (float)data.EFF.Value;
            //    }
            //    if (data.SCRAP != null)
            //    {
            //        SCRAPValues[data.DATE_.Value] = (float)data.SCRAP.Value;
            //    }
            //    if (data.DSA_ITEM != null)
            //    {
            //        DSA_ITEMValues[data.DATE_.Value] = (float)data.DSA_ITEM.Value;
            //    }
            //    if (data.DSA_VOLUME != null)
            //    {
            //        DSA_VOLUMEValues[data.DATE_.Value] = (float)data.DSA_VOLUME.Value;
            //    }
            //}

            // TEST: 
            //ViewBag.Audit5SValues = audit5SValues;
            //ViewBag.complianceValues = complianceValues;
            //ViewBag.ehs_patrolValues = ehs_patrolValues;
            //ViewBag.LBO_PO_PCSValues = LBO_PO_PCSValues;
            //ViewBag.LBO_PO_PPMValues = LBO_PO_PPMValues;
            //ViewBag.EFFValues = EFFValues;
            //ViewBag.SCRAPValues = SCRAPValues;
            //ViewBag.DSA_ITEMValues = DSA_ITEMValues;
            //ViewBag.DSA_VOLUMEValues = DSA_VOLUMEValues;


            // ======================================================================= //


            // Create a list of dates from Monday to Saturday
            List<DateTime> dates = new List<DateTime>();

            // Start from Monday to Saturday
            DateTime currentDay = weekEnding.AddDays(-5);
            for (int i = 1; i < 6; i++)
            {
                dates.Add(currentDay);
                currentDay = currentDay.AddDays(1);
            }

            // Add Saturday to the list of dates
            dates.Add(weekEnding);

            // Assign values to ViewBag
            ViewBag.Dates = dates;
            ViewBag.KPIs = new Dictionary<string, Dictionary<DateTime, float>>
            {
                { "AUDIT 5S", audit5SValues },
                { "COMPLIANCE", complianceValues },
                { "EHS PATROL", ehs_patrolValues },
                { "LBO PO (PCS)", LBO_PO_PCSValues },
                { "LBO PO (PPM)", LBO_PO_PPMValues },
                { "EFFICIENCY", EFFValues },
                { "SCRAP", SCRAPValues },
                { "DSA ITEM", DSA_ITEMValues },
                { "DSA VOLUME", DSA_VOLUMEValues },
                // Add more KPIs here...
            };


            // Calculate WTD and average WTD values (same as before)
            Dictionary<string, Dictionary<DateTime, float>> wtdValues = new Dictionary<string, Dictionary<DateTime, float>>();
            foreach (var kvp in ViewBag.KPIs)
            {
                var kpiName = kvp.Key;
                var kpiValues = kvp.Value;

                Dictionary<DateTime, float> wtdKpiValues = CalculateTotalWTD(weekEnding, kpiValues);

                wtdValues.Add(kpiName, wtdKpiValues);
            }
            // Calculate average values for WTD (same as before)
            Dictionary<string, float> averageWtdValues = new Dictionary<string, float>();
            foreach (var kvp in wtdValues)
            {
                var kpiName = kvp.Key;
                var kpiValues = kvp.Value;
                float average = 0;

                if (kpiValues.Count > 0)
                {
                    if (kpiName == "LBO PO (PCS)")
                    {
                        average = kpiValues.Values.Sum();
                    }
                    else
                    {
                        average = kpiValues.Values.Average();

                    }
                    averageWtdValues.Add(kpiName, average);
                }
            }
            // Assign average WTD and LWP values to ViewBag
            ViewBag.AverageWtdValues = averageWtdValues;

            //
            // ========================= LAST WEEK PERFORMANCE================================ //
            //

            // Calculate previous week ending
            DateTime previousWeekEnding = weekEnding.AddDays(-7);

            DateTime LWP_startDate = DateTime.Now.Date;
            DateTime LWP_endDate = DateTime.Now.Date;
            if (previousWeekEnding != null)
            {
                LWP_startDate = previousWeekEnding.AddDays(-6);
                LWP_endDate = previousWeekEnding;
            }
            else
            {
                LWP_startDate = currentDate.HasValue ? currentDate.Value.AddDays(-6) : DateTime.Now.Date.AddDays(-6);
                LWP_endDate = currentDate ?? DateTime.Now.Date;
            }

            ViewBag.LWP_startDate = LWP_startDate;
            ViewBag.LWP_endDate = LWP_endDate;
            ViewBag.previousWeekEnding = previousWeekEnding.ToShortDateString();

            // Display data KPI Performance
            var data_kpi_lwp = ssc_db.pim_kpi_performance
                .Where(x => x.DATE_ >= LWP_startDate && x.DATE_ <= LWP_endDate)
                .ToList();

            Dictionary<DateTime, float> audit5SValues_lwp = new Dictionary<DateTime, float>();
            Dictionary<DateTime, float> complianceValues_lwp = new Dictionary<DateTime, float>();
            Dictionary<DateTime, float> ehs_patrolValues_lwp = new Dictionary<DateTime, float>();
            Dictionary<DateTime, float> LBO_PO_PCSValues_lwp = new Dictionary<DateTime, float>();
            Dictionary<DateTime, float> LBO_PO_PPMValues_lwp = new Dictionary<DateTime, float>();
            Dictionary<DateTime, float> EFFValues_lwp = new Dictionary<DateTime, float>();
            Dictionary<DateTime, float> SCRAPValues_lwp = new Dictionary<DateTime, float>();
            Dictionary<DateTime, float> DSA_ITEMValues_lwp = new Dictionary<DateTime, float>();
            Dictionary<DateTime, float> DSA_VOLUMEValues_lwp = new Dictionary<DateTime, float>();

            foreach (var data in data_kpi_lwp)
            {
                if (data.AUDIT_5S != null)
                {
                    audit5SValues_lwp[data.DATE_.Value] = (float)data.AUDIT_5S.Value;
                }
                if (data.COMPLIANCE != null)
                {
                    complianceValues_lwp[data.DATE_.Value] = (float)data.COMPLIANCE.Value;
                }
                if (data.EHS_PATROL != null)
                {
                    ehs_patrolValues_lwp[data.DATE_.Value] = (float)data.EHS_PATROL.Value;
                }
                if (data.LBO_PO_PCS != null)
                {
                    LBO_PO_PCSValues_lwp[data.DATE_.Value] = (float)data.LBO_PO_PCS.Value;
                }
                if (data.LBO_PO_PPM != null)
                {
                    LBO_PO_PPMValues_lwp[data.DATE_.Value] = (float)data.LBO_PO_PPM.Value;
                }
                if (data.EFF != null)
                {
                    EFFValues_lwp[data.DATE_.Value] = (float)data.EFF.Value;
                }
                if (data.SCRAP != null)
                {
                    SCRAPValues_lwp[data.DATE_.Value] = (float)data.SCRAP.Value;
                }
                if (data.DSA_ITEM != null)
                {
                    DSA_ITEMValues_lwp[data.DATE_.Value] = (float)data.DSA_ITEM.Value;
                }
                if (data.DSA_VOLUME != null)
                {
                    DSA_VOLUMEValues_lwp[data.DATE_.Value] = (float)data.DSA_VOLUME.Value;
                }
            }
            // Assign values_lwp to ViewBag
            ViewBag.Dates = dates;
            ViewBag.LWP_KPIs = new Dictionary<string, Dictionary<DateTime, float>>
            {
                { "AUDIT 5S", audit5SValues_lwp },
                { "COMPLIANCE", complianceValues_lwp },
                { "EHS PATROL", ehs_patrolValues_lwp },
                { "LBO PO (PCS)", LBO_PO_PCSValues_lwp },
                { "LBO PO (PPM)", LBO_PO_PPMValues_lwp },
                { "EFFICIENCY", EFFValues_lwp },
                { "SCRAP", SCRAPValues_lwp },
                { "DSA ITEM", DSA_ITEMValues_lwp },
                { "DSA VOLUME", DSA_VOLUMEValues_lwp },
                // Add more LWP KPIs here...
            };

            // Calculate WTD and average WTD values (same as before)
            Dictionary<string, Dictionary<DateTime, float>> lwpValues = new Dictionary<string, Dictionary<DateTime, float>>();
            foreach (var kvp in ViewBag.LWP_KPIs)
            {
                var kpiName = kvp.Key;
                var kpiValues = kvp.Value;

                Dictionary<DateTime, float> lwpKpiValues = CalculateAverageLWP(previousWeekEnding, kpiValues);

                lwpValues.Add(kpiName, lwpKpiValues);
            }
            // Calculate average values for WTD (same as before)
            Dictionary<string, float> averageLwpValues = new Dictionary<string, float>();
            foreach (var kvp in lwpValues)
            {
                var kpiName = kvp.Key;
                var kpiValues = kvp.Value;
                float average = 0;

                if (kpiValues.Count > 0)
                {
                    if (kpiName == "LBO PO (PCS)")
                    {
                        average = kpiValues.Values.Sum();
                    }
                    else
                    {
                        average = kpiValues.Values.Average();

                    }
                    averageLwpValues.Add(kpiName, average);
                }
            }
            // Assign average WTD and LWP values to ViewBag
            ViewBag.AverageLwpValues = averageLwpValues;


            return View(data_kpi_day);
        }
        private Dictionary<string, float> GetLwpDataForWeek(DateTime weekEnding)
        {
            Dictionary<string, float> lwpData = new Dictionary<string, float>();

            // Calculate the week starting date by subtracting 7 days from weekEnding
            DateTime weekStarting = weekEnding.AddDays(-7);

            // Fetch LWP data from your data source based on the specified week starting and ending dates
            // Here, you need to replace this with your actual data retrieval logic

            // Assuming you have a database context named "ssc_db" and an entity named "LwpData"

            var lwpQuery = ssc_db.pim_kpi_performance
                .Where(data => data.DATE_ == weekStarting && data.DATE_ == weekEnding)
                .ToList();



            return lwpData;
        }

        private Dictionary<DateTime, float> CalculateAverageLWP(DateTime previousWeekEnding, Dictionary<DateTime, float> values)
        {
            var filteredValues = values.Where(kvp => kvp.Key >= previousWeekEnding.AddDays(-6) && kvp.Key <= previousWeekEnding);
            return filteredValues.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        private Dictionary<DateTime, float> CalculateTotalWTD(DateTime currentWeekEnding, Dictionary<DateTime, float> values)
        {
            var filteredValues = values.Where(kvp => kvp.Key <= currentWeekEnding);
            return filteredValues.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

    }


}
