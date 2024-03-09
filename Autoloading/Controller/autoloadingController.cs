using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.UI.WebControls;
using static System.Data.Entity.Infrastructure.Design.Executor;

namespace OEE_SSC.Models
{
    public class autoloadingController : Controller
    {

        // START - ENTITIES DATA MODEL //
        autoloadEntities auto_load = new autoloadEntities(); // autoloading operator db


        public ActionResult Menu()
        {
            return View();
        }

        // GET: autoloading
        public ActionResult Index(string LEADERNAME = null)
        {
            // Initialize an empty list for operator data
            var dataOpt = new List<pim_autoloading>();


            // >>> LIST OF WEEK ENDING <<< //
            var dataLead = auto_load.pim_autoloading
                .Where(x => x.EMNAME != null)
                .Select(x => x.LEADERNAME)
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            //var dataOpt = auto_load.pim_autoloading.OrderBy(x => x.LEADERNAME).ThenBy(x => x.EMNAME).ThenBy(x => x.SKILL).ToList();
            if (LEADERNAME == null)
            {

                // grup berdasarkan leader 
                dataOpt = auto_load.pim_autoloading.Where(x => x.EMNAME != null)
               .GroupBy(x => new { x.EMEMP_, x.EMNAME, x.LEADERNAME })
               .Select(g => new
               {
                   EMEMP_ = g.Key.EMEMP_,
                   EMNAME = g.Key.EMNAME,
                   LEADERNAME = g.Key.LEADERNAME
               }).
               OrderBy(x => x.LEADERNAME).ThenBy(x => x.EMNAME).ToList()
               .Select(x => new pim_autoloading
               {
                   EMEMP_ = x.EMEMP_,
                   EMNAME = x.EMNAME,
                   LEADERNAME = x.LEADERNAME
               }).ToList();


            }
            else
            {
                // grup berdasarkan leader 
                dataOpt = auto_load.pim_autoloading.Where(x => x.EMNAME != null && x.LEADERNAME == LEADERNAME)
               .GroupBy(x => new { x.EMEMP_, x.EMNAME, x.LEADERNAME })
               .Select(g => new
               {
                   EMEMP_ = g.Key.EMEMP_,
                   EMNAME = g.Key.EMNAME,
                   LEADERNAME = g.Key.LEADERNAME
               }).
               OrderBy(x => x.LEADERNAME).ThenBy(x => x.EMNAME).ToList()
               .Select(x => new pim_autoloading
               {
                   EMEMP_ = x.EMEMP_,
                   EMNAME = x.EMNAME,
                   LEADERNAME = x.LEADERNAME
               }).ToList();


            }
            var table_data = new pimListData
            {
                // database operator
                pim_Autoloadings = dataOpt.ToList(),
                // Mengisi dengan daftar 'LEADERNAME'
                pim_Autoloadings_leader = dataLead,
            };

            return View(table_data);
        }


        // GET: autoloading/Details/5
        public ActionResult Details(int EMEMP_)
        {
            var table_autoloading = new pimListData
            {
                // database auto loading opt
                pim_Autoloadings = auto_load.pim_autoloading.Where(x => x.EMEMP_ == EMEMP_).OrderBy(x => x.LEADERNAME).ThenBy(x => x.EMNAME).ThenBy(x => x.SKILL).ToList(),
                // database auto loading skill list
                pim_Autoloading_Skills = auto_load.pim_autoloading_skill.OrderBy(x => x.SKILLCODE).ThenBy(x => x.SKILLNAME).ToList()
            };

            var data_opt = table_autoloading.pim_Autoloadings.Where(x => x.EMEMP_ == EMEMP_).FirstOrDefault();

            ViewBag.optname = data_opt.EMNAME;
            ViewBag.EMEMP_ = data_opt.EMEMP_;

            return View(table_autoloading);
        }

        // GET: autoloading/Create
        public ActionResult addSKILL(int EMEMP_ = 0)
        {
            var dataLoading = auto_load.pim_autoloading.Where(x => x.EMNAME != null && x.EMEMP_ == EMEMP_).FirstOrDefault();
            var dataLead = auto_load.pim_autoloading.Where(x => x.EMNAME != null).Select(x => x.LEADERNAME).Distinct().ToList();
            var data_skill_code = auto_load.pim_autoloading_skill.Select(x => x.SKILLCODE).Distinct().ToList();
            var data_Skills = auto_load.pim_autoloading_skill.OrderBy(x => x.SKILLCODE).ThenBy(x => x.SKILLNAME).ToList();


            // Simpan Value yang ditampikan
            if (EMEMP_ != 0)
            {
                ViewBag.EMEMP_ = dataLoading.EMEMP_;
                ViewBag.EMNAME = dataLoading.EMNAME;
                ViewBag.LEADERNAME = dataLoading.LEADERNAME;
                ViewBag.SKILL = dataLoading.SKILL;
                ViewBag.opt_notes = dataLoading.opt_notes;

                ViewBag.SKILL = dataLoading.SKILL;

                ViewBag.EMEMP_ = EMEMP_;

            }

            var findSkills = auto_load.pim_autoloading
                .Where(x => x.EMNAME != null && x.EMEMP_ == EMEMP_)
                .Select(x => x.SKILL)
                .ToList();


            // Menyimpan nilai-nilai dalam array
            //string[] findSkillsValues = findSkills;

            // Menyimpan array dalam ViewBag
            ViewBag.findSkillsValues = findSkills;

            var table_autoloading = new pimListData
            {
                // database auto loading skill list
                pim_Autoloading_Skillcodes = data_skill_code,
                pim_Autoloading_Skills = data_Skills,
                // Mengisi dengan daftar 'LEADERNAME'
                pim_Autoloadings_leader = dataLead,
            };

            return View(table_autoloading);
        }

        // POST: autoloading/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult addSKILL(pim_autoloading model, int EMEMP_, string EMNAME = null, string LEADERNAME = null, string opt_notes = null, string SKILL_1A = null, string SKILL_1B = null, string SKILL_1C = null, string SKILL_1D = null, string SKILL_2A = null, string SKILL_2B = null, string SKILL_2C = null, string SKILL_2D = null, string SKILL_3A = null, string SKILL_3B = null, string SKILL_3C = null, string SKILL_4 = null)
        {
            // Your existing code...
            // skill rank / priority
            var rankMap = new Dictionary<string, int>
            {
                { "SKILL_1A", 10 },
                { "SKILL_1B", 3 },
                { "SKILL_1C", 1 },
                { "SKILL_1D", 2 },
                { "SKILL_2A", 11 },
                { "SKILL_2B", 9 },
                { "SKILL_2C", 5 },
                { "SKILL_2D", 8 },
                { "SKILL_3A", 4 },
                { "SKILL_3B", 6 },
                { "SKILL_3C", 7 },
                { "SKILL_4", 12 }
            };

            int rank_ = 0;
            foreach (var skill in rankMap.Keys)
            {
                if (Request.Form[skill] != null)
                {
                    rank_ = rankMap[skill];
                    break;
                }
            }

            var dataRecords = auto_load.pim_autoloading.Where(x => x.EMEMP_ == EMEMP_).ToList();

            if (dataRecords != null)
            {
                // Find and delete existing records with the same EMEMP_
                foreach (var record in dataRecords)
                {
                    auto_load.pim_autoloading.Remove(record);
                }
                auto_load.SaveChanges();
            }

            // Add new data to the 'pim_autoloading' table for each skill
            string[] optSKILL = new string[] { SKILL_1A, SKILL_1B, SKILL_1C, SKILL_1D, SKILL_2A, SKILL_2B, SKILL_2C, SKILL_2D, SKILL_3A, SKILL_3B, SKILL_3C, SKILL_4 };
            for (int hourCounter = 0; hourCounter < optSKILL.Length; hourCounter++)
            {
                string operatorSKILL = optSKILL[hourCounter];
                if (!string.IsNullOrEmpty(operatorSKILL))
                {
                    auto_load.pim_autoloading.Add(new pim_autoloading
                    {
                        rank_skill = rank_,
                        EMEMP_ = EMEMP_,
                        EMNAME = EMNAME,
                        LEADERNAME = LEADERNAME,
                        SKILL = operatorSKILL,
                        opt_notes = opt_notes
                    });
                }

                auto_load.SaveChanges();
            }


            return RedirectToAction("Index", new { LEADERNAME = LEADERNAME });
        }


        // GET: autoloading/Edit/5
        public ActionResult skillEdit(int id)
        {
            return View();
        }

        // POST: autoloading/Edit/5
        [HttpPost]
        public ActionResult skillEdit(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add update logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return RedirectToAction("Details");
            }
        }

        // GET: autoloading/Delete/5
        public ActionResult deleteData(int EMEMP_)
        {
            // Directly call the deleteDataConfirmed action
            return RedirectToAction("deleteDataConfirmed", new { EMEMP_ = EMEMP_ });
        }
        // GET: autoloading/DeleteConfirmed/5
        public ActionResult deleteDataConfirmed(int EMEMP_)
        {
            // TODO: Add delete logic here

            // Cari semua data berdasarkan kpk
            var data_operators = auto_load.pim_autoloading.Where(op => op.EMEMP_ == EMEMP_).ToList();

            if (data_operators.Count == 0)
            {
                return HttpNotFound();
            }

            // Hapus semua baris dengan EMEMP_ yang sesuai
            auto_load.pim_autoloading.RemoveRange(data_operators);
            auto_load.SaveChanges();

            TempData["Message"] = "Data Skill Operator dengan KPK = \"" + EMEMP_ + "\" telah berhasil dihapus.";

            return RedirectToAction("Index");
        }


        // EXPORT SCRAP NOTICE - EXPORT TO EXCEL
        [HttpGet]
        public ActionResult exportDatabaseSkill()
        {
            var databaseOPT = auto_load.pim_autoloading.Where(x => x.EMNAME != null && x.LEADERNAME != null).
                OrderBy(x => x.LEADERNAME).ThenBy(x => x.SKILL).ToList();

            if (databaseOPT == null)
            {
                return HttpNotFound();
            }

            // Membuat file Excel menggunakan library EPPlus
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var package = new ExcelPackage())
            {
                // Membuat worksheet baru
                var worksheet = package.Workbook.Worksheets.Add("skill");

                // Judul Worksheet (+enter, row 1,2 )
                //worksheet.Cells[1, 2].Value = "SCRAP DATA";
                // Tangga/Shift (+enter, row 3, 4)

                // start from row 5
                // Menambahkan header kolom
                worksheet.Cells[1, 1].Value = "EMEMP#";
                worksheet.Cells[1, 2].Value = "EMNAME";
                worksheet.Cells[1, 3].Value = "SKILL";
                worksheet.Cells[1, 4].Value = "LEADER";
                worksheet.Cells[1, 5].Value = "UNIQUE";
                worksheet.Cells[1, 6].Value = "SCHEDULED";
                worksheet.Cells[1, 7].Value = "RANK1";
                worksheet.Cells[1, 8].Value = "MARKING";
                worksheet.Cells[1, 9].Value = "OPERATOR MARK";
                worksheet.Cells[1, 10].Value = "OPERATOR COUNT";
                worksheet.Cells[1, 11].Value = "RANK_SKILL";
                worksheet.Cells[1, 12].Value = "NOTES";

                // Menambahkan data scrap ke worksheet
                // continue from line 3
                //int row = 2;
                int row = 2;
                //int count = 1;
                foreach (var opt in databaseOPT)
                {
                    //worksheet.Cells[row, 1].Value = count;
                    worksheet.Cells[row, 1].Value = opt.EMEMP_;
                    worksheet.Cells[row, 2].Value = opt.EMNAME;
                    worksheet.Cells[row, 3].Value = opt.SKILL;
                    worksheet.Cells[row, 4].Value = opt.LEADERNAME;
                    worksheet.Cells[row, 5].Value = opt.UNIQUE;
                    worksheet.Cells[row, 6].Value = opt.SCHEDULED;
                    if (row == 2)
                    {
                        worksheet.Cells[row, 7].Value = "IF(J2>0,0,COUNTIFS($C$2:C2,C2, $D$2:D2,D2,$J$2:J2,\"<>1\"))";
                        worksheet.Cells[row, 8].Value = "C2&G2";
                        worksheet.Cells[row, 9].Value = "IF(F2>0,B2,0)";
                        worksheet.Cells[row, 10].Value = "COUNTIF(I:I,B2)";
                    }
                    else
                    {
                        worksheet.Cells[row, 7].Value = opt.rank1;
                        worksheet.Cells[row, 8].Value = opt.marking;
                        worksheet.Cells[row, 9].Value = opt.operator_mark;
                        worksheet.Cells[row, 10].Value = opt.operator_count;
                    }
                    worksheet.Cells[row, 11].Value = opt.rank_skill;
                    worksheet.Cells[row, 12].Value = opt.opt_notes;

                    row++;
                    //count++;
                }

                // Mengatur lebar kolom otomatis
                worksheet.Cells.AutoFitColumns();

                // Menyimpan file Excel ke MemoryStream
                var memoryStream = new MemoryStream();
                package.SaveAs(memoryStream);
                memoryStream.Position = 0;

                // Mengirimkan file Excel sebagai unduhan ke pengguna
                var excelName = "Database_Skill_Operator.xlsx";
                var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                return File(memoryStream, contentType, excelName);
            }
        }


    }
}
