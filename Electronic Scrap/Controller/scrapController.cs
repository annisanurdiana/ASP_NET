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
using System.Web.UI.WebControls.WebParts;
using Antlr.Runtime.Misc;


namespace OEE_SSC.Controllers
{
    public class scrapController : Controller
    {
        // START - ENTITIES DATA MODEL //
        MDSEntities pn_db = new MDSEntities(); // partnumber_official db
        masbroEntitiesScrap scrap_db = new masbroEntitiesScrap(); // pim_scrap db

        // GET: scrap
        public ActionResult Index()
        {
            return View();
        }

        // -----------------------------------------------SSC dan INPUT SCRAB----------------------------------------------- //
        public ActionResult firstPage(int shift_ = 0, DateTime? date_ = null)
        {
            DateTime currentDate = date_ ?? DateTime.Now;
            // Jika waktu saat ini sudah melebihi jam 22.40, tambahkan 1 hari ke tanggal saat ini
            if (currentDate.Hour >= 22 && currentDate.Minute >= 40)
            {
                currentDate = currentDate.AddDays(1);
            }

            // Konversi tanggal ke format string "yyyy-MM-dd"
            string dateString = currentDate.ToShortDateString();

            Session["scrapDate"] = date_;
            Session["scrapShift"] = shift_;


            ViewBag.ErrorImport = TempData["ErrorImport"];
            ViewBag.MessageImport = TempData["MessageImport"];

            // jika shiftEmp tidak diisi atau (dateFilter == null dan n_machine kosong)
            if (shift_ == 0 || (date_ == null))
            {
                return View();
            }
            // ---- jika shift dan dateFilter diisi ---- //
            else
            {

                // simpan nilai shiftEmp ke dalam ViewBag atau ViewData
                ViewBag.ShiftEmp = shift_;

                var data1 = scrap_db.pim_scrap.
                    Where(x => x.shift_ == shift_ && x.date_ == date_).
                    OrderBy(x => x.partnumber).
                    ToList();

                // jika ada data yang ditemukan, tampilkan view tes_input dengan menggunakan data tersebut
                if (data1.Any())
                {
                    return View("inputScrap", data1);
                }
                else
                {
                    TempData["Message"] = "Scrap Data on Date: " + date_ + " Shift: " + shift_ + " Not Found!"; // Pesan sukses
                    return View("inputScrap", data1);
                }
            }
        }

        // Add New Data REJECT from user to database table pim_user_output
        [HttpGet]
        public ActionResult inputScrap(int shift_ = 0, DateTime? date_ = null)
        {
            //int shift_ = (int)System.Web.HttpContext.Current.Session["scrapDate"];
            //DateTime date_ = (DateTime)System.Web.HttpContext.Current.Session["scrapShift"];

            if (shift_ == 0 || (date_ == null))
            {
                return View();
            }
            else
            {
                var data_scrap = scrap_db.pim_scrap.
                    Where(x => x.shift_ == shift_ && x.date_ == date_).
                    OrderBy(x => x.partnumber).
                    ToList();

                return View(data_scrap);

            }
        }
        [HttpPost]
        public ActionResult inputScrap(pim_scrap model)
        {
            // Insert data
            scrap_db.pim_scrap.Add(model);
            scrap_db.SaveChanges();
            ViewBag.Message = "Reject Data Insert Successfully!";

            // Redirect to innerDataMC with the updated user_id
            return RedirectToAction("inputScrap");
        }

        // DELETE SCRAP
        [HttpGet]
        public ActionResult deleteScrap(int id)
        {
            // SESSION
            int scrapShift = (int)(System.Web.HttpContext.Current.Session["scrapShift"] ?? 0);
            DateTime? scrapDate = (DateTime?)System.Web.HttpContext.Current.Session["scrapDate"];

            var scrap = scrap_db.pim_scrap.Find(id); // Temukan data scrap berdasarkan ID

            if (scrap != null)
            {
                scrap_db.pim_scrap.Remove(scrap); // Hapus data scrap dari database
                scrap_db.SaveChanges(); // Simpan perubahan

                TempData["Message"] = "Scrap data berhasil di hapus"; // Pesan sukses
            }
            else
            {
                TempData["Error_scrap"] = "Gagal data tidak dapat ditemukan.";
            }

            // Redirect kembali ke halaman inputScrap
            return RedirectToAction("inputScrap", new { date_ = scrapDate, shift_ = scrapShift });
        }

        // EXPORT SCRAP NOTICE - EXPORT TO EXCEL
        [HttpGet]
        public ActionResult exportCurrentScrap(int shift_ = 0, DateTime? dateStart = null, DateTime? dateEnd = null)
        {

            DateTime now = DateTime.Now;
            DateTime startDate = dateStart ?? now.Date;
            DateTime finishDate = dateEnd ?? now.Date;

            var data_scrap = scrap_db.pim_scrap.ToList();
            
            if (shift_ == 0)
            {
                data_scrap = scrap_db.pim_scrap.Where(x => x.date_ >= startDate && x.date_ <= finishDate).OrderBy(x => x.date_).ThenBy(x => x.shift_).ThenByDescending(x => x.quantity).ToList();
            }
            else
            {
                data_scrap = scrap_db.pim_scrap.Where(x => x.shift_ == shift_ && x.date_ >= startDate && x.date_ <= finishDate).OrderBy(x => x.date_).ThenByDescending(x => x.quantity).ToList();
            }

            if (data_scrap == null)
            {
                return HttpNotFound();
            }

            // Membuat file Excel menggunakan library EPPlus
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var package = new ExcelPackage())
            {
                // Membuat worksheet baru
                var worksheet = package.Workbook.Worksheets.Add("Scrap Data");

                // Judul Worksheet (+enter, row 1,2 )
                //worksheet.Cells[1, 2].Value = "SCRAP DATA";
                // Tangga/Shift (+enter, row 3, 4)

                // start from row 5
                // Menambahkan header kolom
                worksheet.Cells[1, 1].Value = "NO";
                worksheet.Cells[1, 2].Value = "DATE";
                worksheet.Cells[1, 3].Value = "AREA";
                worksheet.Cells[1, 4].Value = "SHIFT";
                worksheet.Cells[1, 5].Value = "PART NUMBER";
                worksheet.Cells[1, 6].Value = "QTY";
                worksheet.Cells[1, 7].Value = "CODE";
                worksheet.Cells[1, 8].Value = "STATUS";

                // Menambahkan data scrap ke worksheet
                // continue from line 3
                //int row = 2;
                int row = 2;
                int count = 1;
                foreach (var scrap in data_scrap)
                {
                    worksheet.Cells[row, 1].Value = count;
                    worksheet.Cells[row, 2].Value = scrap.date_;
                    worksheet.Cells[row, 3].Value = scrap.area_name;
                    worksheet.Cells[row, 4].Value = scrap.shift_;
                    worksheet.Cells[row, 5].Value = scrap.partnumber;
                    worksheet.Cells[row, 6].Value = scrap.quantity;
                    worksheet.Cells[row, 7].Value = scrap.scrap_number + scrap.scrap_code; // U/M
                    worksheet.Cells[row, 8].Value = scrap.check_;

                    row++;
                    count++;
                }


                // Mengatur lebar kolom otomatis
                worksheet.Cells.AutoFitColumns();

                // Menyimpan file Excel ke MemoryStream
                var memoryStream = new MemoryStream();
                package.SaveAs(memoryStream);
                memoryStream.Position = 0;

                // Mengirimkan file Excel sebagai unduhan ke pengguna
                var excelName = "Scrap_" + dateEnd + "/" + shift_ + ".xlsx";
                var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                return File(memoryStream, contentType, excelName);
            }
        }

        // EXPORT SCRAP Audit - EXPORT TO EXCEL
        [HttpGet]
        public ActionResult exportScrapAudit(int shift_ = 0, DateTime? date_ = null)
        {
            // SESSION
            int scrapShift = (int)(System.Web.HttpContext.Current.Session["scrapShift"] ?? 0);
            DateTime? scrapDate = (DateTime?)System.Web.HttpContext.Current.Session["scrapDate"];

            DateTime now = DateTime.Now;

            DateTime prevDate;

            // Handle the case where scrapDate is null
            if (scrapDate.HasValue)
            {
                // scrapDate has a value, so we can safely use Value property to get the DateTime
                prevDate = scrapDate.Value.Date.AddDays(-1);
            }
            else
            {
                // Handle the case where scrapDate is null
                // You might want to provide a default value or take appropriate action
                prevDate = now.Date.AddDays(-1);
            }

            ViewBag.prevDate = prevDate;

            var prev_data_scrap = scrap_db.pim_scrap.ToList();
            var data_scrap_shift1 = scrap_db.pim_scrap.ToList();


            prev_data_scrap = scrap_db.pim_scrap.Where(x => x.date_ == prevDate && (x.shift_ == 2 || x.shift_ == 3)).OrderBy(x => x.shift_).ThenByDescending(x => x.quantity).ToList();
            data_scrap_shift1 = scrap_db.pim_scrap.Where(x => x.shift_ == 1 && x.date_ == scrapDate).OrderBy(x => x.quantity).ToList();


            // Membuat file Excel menggunakan library EPPlus
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var package = new ExcelPackage())
            {
                // Membuat worksheet baru
                var worksheet = package.Workbook.Worksheets.Add("Scrap Data");

                // start from row 5
                // Menambahkan header kolom
                worksheet.Cells[1, 1].Value = "NO";
                worksheet.Cells[1, 2].Value = "DATE";
                worksheet.Cells[1, 3].Value = "AREA";
                worksheet.Cells[1, 4].Value = "SHIFT";
                worksheet.Cells[1, 5].Value = "PART NUMBER";
                worksheet.Cells[1, 6].Value = "QTY";
                worksheet.Cells[1, 7].Value = "CODE";
                worksheet.Cells[1, 8].Value = "STATUS";


                // Menambahkan data scrap ke worksheet
                // continue from line 3
                //int row = 2;
                int row = 2;
                int count = 1;

                // Tanggal kemarin dan shift 2 & 3
                foreach (var scrap in prev_data_scrap)
                {
                    worksheet.Cells[row, 1].Value = count;
                    worksheet.Cells[row, 2].Value = scrap.date_;
                    worksheet.Cells[row, 3].Value = scrap.area_name;
                    worksheet.Cells[row, 4].Value = scrap.shift_;
                    worksheet.Cells[row, 5].Value = scrap.partnumber;
                    worksheet.Cells[row, 6].Value = scrap.quantity;
                    worksheet.Cells[row, 7].Value = scrap.scrap_number + scrap.scrap_code; // U/M
                    worksheet.Cells[row, 8].Value = scrap.check_;

                    row++;
                    count++;
                }

                // Tanggal hari ini dan shift 1
                int row2 = row;
                foreach (var scrap in data_scrap_shift1)
                {
                    worksheet.Cells[row2, 1].Value = count;
                    worksheet.Cells[row2, 2].Value = scrap.date_;
                    worksheet.Cells[row2, 3].Value = scrap.area_name;
                    worksheet.Cells[row2, 4].Value = scrap.shift_;
                    worksheet.Cells[row2, 5].Value = scrap.partnumber;
                    worksheet.Cells[row2, 6].Value = scrap.quantity;
                    worksheet.Cells[row2, 7].Value = scrap.scrap_number + scrap.scrap_code; // U/M
                    worksheet.Cells[row2, 8].Value = scrap.check_;

                    row2++;
                    count++;
                }

                // Mengatur lebar kolom otomatis
                worksheet.Cells.AutoFitColumns();

                // Menyimpan file Excel ke MemoryStream
                var memoryStream = new MemoryStream();
                package.SaveAs(memoryStream);
                memoryStream.Position = 0;

                // Mengirimkan file Excel sebagai unduhan ke pengguna
                var excelName = "Scrap_Audit" + ".xlsx";
                var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                return File(memoryStream, contentType, excelName);
            }
        }

        // EXPORT SCRAP NOTICE - EXPORT TO EXCEL
        [HttpGet]
        public ActionResult exportScrapExcel(int shift_ = 0, DateTime? date_ = null)
        {
            var data_scrap = scrap_db.pim_scrap.
                Where(x => x.shift_ == shift_ && x.date_ == date_).
                OrderBy(x => x.partnumber).
                ToList();

            if (data_scrap == null)
            {
                return HttpNotFound();
            }

            // Mendapatkan data scrap dari database
            //var dataScrap = data_scrap.OrderBy(x => x.partnumber).ToList();


            var dataScrap = scrap_db.pim_scrap.Where(x => x.date_ == date_ && x.shift_ == shift_)
                .GroupBy(x => new { x.date_, x.shift_, x.partnumber, x.scrap_number, x.scrap_code, x.um, x.check_ })
                .Select(g => new
                {
                    date_ = g.Key.date_,
                    shift_ = g.Key.shift_,
                    partnumber = g.Key.partnumber,
                    scrap_number = g.Key.scrap_number,
                    scrap_code = g.Key.scrap_code,
                    um = g.Key.um,
                    check_ = g.Key.check_,
                    quantity = g.Sum(x => x.quantity)
                }).
                OrderBy(x => x.date_).ThenBy(x => x.shift_).ThenBy(x => x.partnumber).ToList()
                .Select(x => new pim_scrap
                {
                    date_ = x.date_,
                    shift_ = x.shift_,
                    partnumber = x.partnumber,
                    scrap_number = x.scrap_number,
                    scrap_code = x.scrap_code,
                    um = x.um,
                    check_ = x.check_,
                    quantity = x.quantity,
                }).ToList();

            var groupedDataScrap = data_scrap
                .GroupBy(x => new { x.date_, x.shift_, x.partnumber, x.scrap_number, x.scrap_code, x.um, x.check_ })
                .Select(g => new
                {
                    date_ = g.Key.date_,
                    shift_ = g.Key.shift_,
                    partnumber = g.Key.partnumber,
                    scrap_number = g.Key.scrap_number,
                    scrap_code = g.Key.scrap_code,
                    um = g.Key.um,
                    check_ = g.Key.check_,
                    quantity = g.Sum(x => x.quantity),
                    total_price = g.Sum(x =>
                    {
                        var scrapQuery = scrap_db.pim_scrap_query.FirstOrDefault(query => query.TOY_NUMBER == x.partnumber);
                        if (scrapQuery != null)
                        {
                            return (decimal)scrapQuery.PRICE_ * x.quantity;
                        }
                        return 0;
                    })
                })
                .OrderBy(x => x.scrap_code)
                .ToList();

            // Membuat file Excel menggunakan library EPPlus
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var package = new ExcelPackage())
            {
                // Membuat worksheet baru
                var worksheet = package.Workbook.Worksheets.Add("Scrap Data");

                // Judul Worksheet (+enter, row 1,2 )
                //worksheet.Cells[1, 2].Value = "SCRAP DATA";
                // Tangga/Shift (+enter, row 3, 4)

                // start from row 5
                // Menambahkan header kolom
                worksheet.Cells[1, 1].Value = "NO";
                worksheet.Cells[1, 2].Value = "DATE";
                worksheet.Cells[1, 3].Value = "SHIFT";
                worksheet.Cells[1, 4].Value = "DOC#";
                worksheet.Cells[1, 5].Value = "NAME SPV";
                worksheet.Cells[1, 6].Value = "PART NUMBER";
                worksheet.Cells[1, 7].Value = "DESCRIPTION";
                worksheet.Cells[1, 8].Value = "U/M";
                worksheet.Cells[1, 9].Value = "QUANTITY";
                worksheet.Cells[1, 10].Value = "RUPIAH";
                worksheet.Cells[1, 11].Value = "WC";
                worksheet.Cells[1, 12].Value = "CODE";
                worksheet.Cells[1, 13].Value = "SCRAP CODE";
                worksheet.Cells[1, 14].Value = "Remarks";
                worksheet.Cells[1, 15].Value = "STATUS";

                // Menambahkan data scrap ke worksheet
                // continue from line 3
                //int row = 2;
                int row = 2;
                int count = 1;
                foreach (var scrap in groupedDataScrap)
                {
                    var code_scrap = scrap_db.pim_scrap_code.FirstOrDefault(x => x.SCRAP_CODE == scrap.scrap_code);
                    var desc_scrap = scrap_db.pim_scrap_query.FirstOrDefault(x => x.TOY_NUMBER == scrap.partnumber);

                    worksheet.Cells[row, 1].Value = count;
                    worksheet.Cells[row, 2].Value = scrap.date_;
                    worksheet.Cells[row, 3].Value = scrap.shift_;
                    worksheet.Cells[row, 4].Value = ""; // DOC#
                    worksheet.Cells[row, 5].Value = ""; // SPV
                    worksheet.Cells[row, 6].Value = scrap.partnumber;
                    worksheet.Cells[row, 7].Value = desc_scrap.DESCRIPTION_;
                    worksheet.Cells[row, 8].Value = scrap.um; // U/M
                    worksheet.Cells[row, 9].Value = scrap.quantity;
                    worksheet.Cells[row, 10].Value = $"{scrap.total_price:N2}";// rupiah
                    worksheet.Cells[row, 11].Value = "100"; // WC
                    worksheet.Cells[row, 12].Value = "IMP"; // CODE
                    worksheet.Cells[row, 13].Value = scrap.scrap_number + scrap.scrap_code;
                    worksheet.Cells[row, 14].Value = code_scrap.SCRAP_REMARKS;
                    worksheet.Cells[row, 15].Value = scrap.check_;

                    row++;
                    count++;
                }

                // Mengatur lebar kolom otomatis
                worksheet.Cells.AutoFitColumns();

                // Menyimpan file Excel ke MemoryStream
                var memoryStream = new MemoryStream();
                package.SaveAs(memoryStream);
                memoryStream.Position = 0;

                // Mengirimkan file Excel sebagai unduhan ke pengguna
                var excelName = "ScrapData.xlsx";
                var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                return File(memoryStream, contentType, excelName);
            }
        }

        // DOWNLOAD PRICE SCRAP - EXPORT TO EXCEL
        [HttpGet]
        public ActionResult DownloadScrapExcel(int shift_ = 0, DateTime? date_ = null)
        {
            var data_scrap = scrap_db.pim_scrap.Where(x => x.date_ == date_ && x.shift_ == shift_ && x.check_ == "key in").ToList();

            var dataScrap = data_scrap
                .GroupBy(x => new { x.scrap_code, x.area_name, x.partnumber, x.check_ })
                .Select(g => new
                {
                    scrap_code = g.Key.scrap_code,
                    check_ = g.Key.check_,
                    area_name = g.Key.area_name,
                    partnumber = g.Key.partnumber,
                    quantity = g.Sum(x => x.quantity)
                }).
                OrderBy(x => x.scrap_code).ThenByDescending(x => x.quantity).ToList()
                .Select(x => new pim_scrap
                {
                    scrap_code = x.scrap_code,
                    check_ = x.check_,
                    area_name = x.area_name,
                    partnumber = x.partnumber,
                    quantity = x.quantity,
                }).ToList();

            var groupedDataScrap = data_scrap
                .GroupBy(x => new { x.scrap_number, x.scrap_code, x.area_name, x.check_ })
                .Select(g => new
                {
                    scrap_code = g.Key.scrap_code,
                    scrap_number = g.Key.scrap_number,
                    area_name = g.Key.area_name,
                    check_ = g.Key.check_,
                    total_quantity = g.Sum(x => x.quantity),
                    total_price = g.Sum(x =>
                    {
                        var scrapQuery = scrap_db.pim_scrap_query.FirstOrDefault(query => query.TOY_NUMBER == x.partnumber);
                        if (scrapQuery != null)
                        {
                            return (decimal)scrapQuery.PRICE_ * x.quantity;
                        }
                        return 0;
                    })
                })
                .OrderBy(x => x.scrap_code).ThenByDescending(x => x.total_price).ThenBy(x => x.area_name)
                .ToList();

            if (dataScrap == null)
            {
                return HttpNotFound();
            }

            // Membuat file Excel menggunakan library EPPlus
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var package = new ExcelPackage())
            {
                // Membuat worksheet baru
                var worksheet = package.Workbook.Worksheets.Add("Scrap Data");

                // Judul Worksheet (+enter, row 1,2 )
                worksheet.Cells[1, 4].Value = "Total Price: ";
                // Tangga/Shift (+enter, row 3, 4)

                // start from row 5
                // Menambahkan header kolom
                worksheet.Cells[3, 1].Value = "No";
                worksheet.Cells[3, 2].Value = "Area Name";
                worksheet.Cells[3, 3].Value = "Scrap Code";
                worksheet.Cells[3, 4].Value = "Scrap Remarks";
                worksheet.Cells[3, 5].Value = "Price";
                worksheet.Cells[3, 6].Value = "Qty";

                // Menambahkan data scrap ke worksheet
                // continue from line 3
                //int row = 2;
                int row = 4;
                int count = 1;
                decimal price_scrap = 0;
                decimal total_price_scrap = 0;

                foreach (var groupedScrap in groupedDataScrap)
                {
                    var code_scrap = scrap_db.pim_scrap_code.FirstOrDefault(x => x.SCRAP_CODE == groupedScrap.scrap_code);

                    worksheet.Cells[row, 1].Value = count;
                    worksheet.Cells[row, 2].Value = groupedScrap.area_name;
                    worksheet.Cells[row, 3].Value = groupedScrap.scrap_number + groupedScrap.scrap_code;
                    worksheet.Cells[row, 4].Value = code_scrap?.SCRAP_REMARKS; // Gunakan ? untuk menghindari NullReferenceException
                    worksheet.Cells[row, 5].Value = $"Rp{groupedScrap.total_price:N2}";
                    worksheet.Cells[row, 6].Value = groupedScrap.total_quantity;

                    row++;
                    count++;

                    if (groupedScrap.total_price.HasValue)
                    {
                        total_price_scrap += groupedScrap.total_price.Value; // Menggunakan .Value untuk mendapatkan nilai dari Nullable<decimal>
                    }
                }


                string formattedTotalPrice = total_price_scrap.ToString("N2");
                worksheet.Cells[1, 5].Value = "Rp" + formattedTotalPrice;

                // Mengatur lebar kolom otomatis
                worksheet.Cells.AutoFitColumns();

                // Menyimpan file Excel ke MemoryStream
                var memoryStream = new MemoryStream();
                package.SaveAs(memoryStream);
                memoryStream.Position = 0;

                DateTime date_2 = date_.GetValueOrDefault(); // Contoh: Tanggal 4 Agustus 2023
                string date_short = date_2.ToString("yyyy-MM-dd"); // Hasilnya: "2023-08-04"

                //string date_format = date_.ToString("yyyy-MM-dd");

                // Mengirimkan file Excel sebagai unduhan ke pengguna
                var excelName = "ScrapPrice on " + date_short + ".xlsx";
                var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                return File(memoryStream, contentType, excelName);
            }
        }

        // EDIT SCRAP - GET
        [HttpGet]
        public ActionResult editScrap(int id)
        {
            // Temukan data scrap berdasarkan ID
            var dataScrap = scrap_db.pim_scrap.Find(id);

            if (dataScrap != null)
            {
                // GENERAL
                ViewBag.id = dataScrap.id;
                ViewBag.date_ = dataScrap.date_;
                ViewBag.shift_ = dataScrap.shift_;
                ViewBag.partnumber = dataScrap.partnumber;
                ViewBag.quantity = dataScrap.quantity;
                ViewBag.scrap_code = dataScrap.scrap_code;
                ViewBag.area_name = dataScrap.area_name;
                ViewBag.area_code = dataScrap.area_code;

                // Tampilkan modal form edit dengan data scrap yang akan diubah
                return View(dataScrap);
            }

            TempData["Error_scrap"] = "Gagal data tidak ditemukan.";  // Pesan kesalahan jika data scrap tidak ditemukan

            // Redirect kembali ke halaman inputScrap
            return RedirectToAction("inputScrap");
        }

        // EDIT SCRAP - POST
        [HttpPost]
        public ActionResult editScrap(pim_scrap model, int code_1 = 0, string code_2 = "")
        {
            var scrap = scrap_db.pim_scrap.Find(model.id); // Temukan data scrap berdasarkan ID

            if (scrap != null)
            {
                // Check if area_code has a value
                if (model.area_code.HasValue)
                {
                    int areaCode = model.area_code.Value;

                    if (model.area_code == 1)
                    {
                        scrap.area_name = "1F - A,B,C,D,M3";
                    }
                    else if (model.area_code == 2)
                    {
                        scrap.area_name = "1F - E,F,G,H";
                    }
                    else if (model.area_code == 3)
                    {
                        scrap.area_name = "1F - J,K,L";
                    }
                    else if (model.area_code == 4)
                    {
                        scrap.area_name = "1F - M,N,P";
                    }
                    else if (model.area_code == 5)
                    {
                        scrap.area_name = "GF - A,B,C,D,E,F";
                    }
                    else if (model.area_code == 6)
                    {
                        scrap.area_name = "GF - G,H,J,K,L,M";
                    }
                    else
                    {
                        scrap.area_name = "Not Found";
                    }
                }
                else
                {
                    scrap.area_name = "NULl";
                }

                // Update dengan field lainnya
                scrap.id = model.id;
                scrap.partnumber = model.partnumber;
                scrap.quantity = model.quantity;
                scrap.scrap_code = model.scrap_code;
                scrap.area_code = model.area_code;

                //scrap_db.Entry(model).State = EntityState.Modified;
                scrap_db.SaveChanges();

                TempData["Message"] = "Data scrap berhasil disimpan"; // Pesan sukses
            }
            else
            {
                TempData["Error_scrap"] = "Data scrap tidak ditemukan."; // Pesan kesalahan jika data scrap tidak ditemukan
            }

            // Redirect kembali ke halaman inputScrap
            return RedirectToAction("inputScrap", new { date_ = model.date_, shift_ = model.shift_ });
        }

        public ActionResult newScrap()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult newScrap(pim_scrap model, int code_1 = 0, string code_2 = null)
        {
            // SESSION
            int scrapShift = (int)(System.Web.HttpContext.Current.Session["scrapShift"] ?? 0);
            DateTime? scrapDate = (DateTime?)System.Web.HttpContext.Current.Session["scrapDate"];
            //DateTime filterDate = dateFilter ?? DateTime.Now.Date;

            var pn_scrap = scrap_db.pim_scrap_query.FirstOrDefault(m => m.TOY_NUMBER == model.partnumber);

            if (pn_scrap == null)
            {
                TempData["Error_add"] = "Part Number tidak valid";
                return RedirectToAction("newScrap", new { date_ = scrapDate, shift_ = scrapShift });
            }

            // Assign the value of date
            //model.date_ = DateTime.Now.Date;
            //model.scrap_number = code_1;


            // Mappings between pim_area_code and pim_area_name
            //Dictionary<int, string> areaCodeToNameMap = new Dictionary<int, string>
            //{
            //    { 1, "1F - A,B,C,D,M3" },
            //    { 2, "1F - E,F,G,H" },
            //    { 3, "1F - J,K,L" },
            //    { 4, "1F - M,N,P" },
            //    { 5, "GF - A,B,C,D,E,F" },
            //    { 6, "GF - G,H,J,K,L,M" }
            //};

            // Check if pim_area_code has a value
            if (model.area_code.HasValue)
            {
                int areaCode = model.area_code.Value;

                if (model.area_code == 1)
                {
                    model.area_name = "1F - A,B,C,D,M3";
                }
                else if (model.area_code == 2)
                {
                    model.area_name = "1F - E,F,G,H";
                }
                else if (model.area_code == 3)
                {
                    model.area_name = "1F - J,K,L";
                }
                else if (model.area_code == 4)
                {
                    model.area_name = "1F - M,N,P";
                }
                else if (model.area_code == 5)
                {
                    model.area_name = "GF - A,B,C,D,E,F";
                }
                else if (model.area_code == 6)
                {
                    model.area_name = "GF - G,H,J,K,L,M";
                }
                else
                {
                    model.area_name = "Not Found";
                }

                // Check if areaCode exists in the map
                //if (areaCodeToNameMap.ContainsKey(areaCode))
                //{
                //    model.area_name = areaCodeToNameMap[areaCode];
                //}
                //else
                //{
                //    model.area_name = "Not Found";
                //}
            }
            else
            {
                model.area_name = "Area Code is null";
            }



            // Insert data
            scrap_db.pim_scrap.Add(model);
            scrap_db.SaveChanges();
            // Update user output data

            var data_scrap = scrap_db.VO_SCRAP.
                Where(x => x.shift_ == scrapShift && x.date_ == scrapDate).
                OrderBy(x => x.partnumber).
                ToList();

            // jika ada data yang ditemukan, tampilkan view tes_input dengan menggunakan data tersebut
            if (data_scrap.Any())
            {
                TempData["Message"] = "Scrap Data Berhasil Disimpan!";
                return RedirectToAction("inputScrap", new { date_ = scrapDate, shift_ = scrapShift });
            }
            else
            {
                return RedirectToAction("inputScrap", new { date_ = scrapDate, shift_ = scrapShift });
            }

        }


        // -----------------------------------IMPORT FROM EXCEL-------------------------------------- //
        // GET: Import 
        public ActionResult importScrapQuery()
        {
            var data_query_scrap = scrap_db.pim_scrap_query.OrderBy(x => x.DATE_INPUT).ThenBy(x => x.TOY_NUMBER).Take(100).ToList();

            // mengirimkan list nomor mesin sebagai model view
            return View(data_query_scrap);
        }

        //POST: Import Scrap Price Data
        [HttpPost]
        public ActionResult importScrapQuery(HttpPostedFileBase file)
        {

            // SESSION
            int scrapShift = (int)(System.Web.HttpContext.Current.Session["scrapShift"] ?? 0);
            DateTime? scrapDate = (DateTime?)System.Web.HttpContext.Current.Session["scrapDate"];

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
                                // create new pim_scrap_query object
                                pim_scrap_query pimScapData = new pim_scrap_query();

                                // set values for FACLWP TOYNWP  PARTWP DESXIT  PLANIT BASPIT  SCRMWP COMMIT  Toy number  Description Harga
                                pimScapData.FACLWP = worksheet.Cells[row, 1].Text?.Trim();
                                pimScapData.TOYNWP = worksheet.Cells[row, 2].Text?.Trim();
                                pimScapData.PARTWP = worksheet.Cells[row, 3].Text?.Trim();
                                pimScapData.DESXIT = worksheet.Cells[row, 4].Text?.Trim();
                                pimScapData.PLANIT = worksheet.Cells[row, 5].Text?.Trim();

                                double.TryParse(worksheet.Cells[row, 6].Value?.ToString(), out double baspitValue);
                                pimScapData.BASPIT = baspitValue;
                                double.TryParse(worksheet.Cells[row, 7].Value?.ToString(), out double scrmwpValue);
                                pimScapData.SCRMWP = scrmwpValue;

                                pimScapData.COMMIT_ = worksheet.Cells[row, 8].Text?.Trim();
                                pimScapData.TOY_NUMBER = worksheet.Cells[row, 9].Text?.Trim();
                                pimScapData.DESCRIPTION_ = worksheet.Cells[row, 10].Text?.Trim();

                                double.TryParse(worksheet.Cells[row, 11].Value?.ToString(), out double priceValue);
                                pimScapData.PRICE_ = priceValue;
                                pimScapData.DATE_INPUT = myDate;


                                pimScapData.ID = pimScapData.TOYNWP + pimScapData.PARTWP;


                                //// Jika data valid dan tidak ada duplikat, maka masukkan ke dalam database
                                //if (!string.IsNullOrEmpty(pimScapData.TOY_NUMBER) && !scrap_db.pim_scrap_query.Any(x => x.TOY_NUMBER == pimScapData.TOY_NUMBER))
                                //{
                                //    scrap_db.pim_scrap_query.Add(pimScapData);
                                //    scrap_db.SaveChanges();
                                //    rowsImported++;
                                //}

                                // If TOY_NUMBER is not empty
                                if (!string.IsNullOrEmpty(pimScapData.TOY_NUMBER))
                                {
                                    // Check if data with the same TOY_NUMBER already exists in the database
                                    var existingDataToyNumber = scrap_db.pim_scrap_query.FirstOrDefault(x => x.TOY_NUMBER == pimScapData.TOY_NUMBER);

                                    if (existingDataToyNumber != null)
                                    {
                                        // If data with the same TOY_NUMBER exists, remove the existing data from the database
                                        scrap_db.pim_scrap_query.Remove(existingDataToyNumber);
                                    }

                                    // Add the new data from Excel to the database
                                    scrap_db.pim_scrap_query.Add(pimScapData);
                                    scrap_db.SaveChanges();
                                    rowsImported++;
                                }
                                else
                                {
                                    ViewBag.Error = "Invalid data. Please check the data in the Excel file.";
                                }
                            }

                            // display success message with the number of imported rows
                            TempData["Message"] = "Import successful: Data scrap berjumlah " + rowsImported + " rows berhasil diperbaharui";

                            // Redirect kembali ke halaman inputScrap
                            return RedirectToAction("inputScrap", new { date_ = scrapDate, shift_ = scrapShift });
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

    }
}