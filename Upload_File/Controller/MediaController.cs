using System;
using System.Collections.Generic;
using System.IO;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OEE_SSC.Models;

namespace OEE_SSC.Controllers
{
    public class MediaController : Controller
    {
        // GET: Media
        public ActionResult Index_test()
        {
            return View();
        }
        public ActionResult Index()
        {
            List<ObjFile> ObjFiles = new List<ObjFile>();
            foreach (string strfile in Directory.GetFiles(Server.MapPath("~/Files")))
            {
                FileInfo fi = new FileInfo(strfile);
                ObjFile obj = new ObjFile();
                obj.File = fi.Name;
                obj.Size = (double)fi.Length / (1024 * 1024); // Convert to MB,  1 MB = 1024 KB, dan 1 KB = 1024 byte 
                obj.Type = GetFileTypeByExtension(fi.Extension);
                ObjFiles.Add(obj);
            }

            return View(ObjFiles);
        }

        [HttpPost]
        public ActionResult Index(HttpPostedFileBase[] files)
        {
            try
            {
                foreach (var file in files)
                {
                    if (file != null && file.ContentLength > 0)
                    {
                        // Langkah 1: Validasi tipe file
                        var allowedFileExtensions = new string[] { ".docx", ".xlsx", ".pdf", ".jpg", ".jpeg", ".png", ".mp4", ".txt" }; // Tipe file yang diizinkan
                        var fileExtension = Path.GetExtension(file.FileName).ToLower();
                        if (!allowedFileExtensions.Contains(fileExtension))
                        {
                            TempData["Message"] = "File type not allowed.";
                            return RedirectToAction("Index");
                        }

                        // Langkah 2: Scan malware dan virus (simulasi)
                        //if (IsInfected(file.InputStream))
                        //{
                        //    TempData["Message"] = "Malware detected in the file.";
                        //    return RedirectToAction("Index");
                        //}

                        // Langkah 3: Simpan di direktori yang aman
                        // var uniqueFileName = Guid.NewGuid().ToString("N") + fileExtension;
                        var filePath = Path.Combine(Server.MapPath("~/Files"), file.FileName);
                        file.SaveAs(filePath);

                        // Langkah 6: Logging
                        //LogFileUpload(User.Identity.Name, uniqueFileName);
                    }
                }

                TempData["Message"] = "Files Uploaded Successfully";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Message"] = "An error occurred: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        public FileResult Download(string fileName)
        {
            string fullPath = Path.Combine(Server.MapPath("~/Files"), fileName);
            byte[] fileBytes = System.IO.File.ReadAllBytes(fullPath);
            return File(fileBytes, System.Net.Mime.MediaTypeNames.Application.Octet, fileName);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteFile(string fileName)
        {
            string fullPath = Path.Combine(Server.MapPath("~/Files"), fileName);
            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
                TempData["Message"] = "File deleted successfully.";
            }
            else
            {
                TempData["Message"] = "File not found.";
            }

            return RedirectToAction("Index");
        }

        // action Open akan mencari file yang sesuai berdasarkan nama file
        public ActionResult Open(string fileName)
        {
            string fullPath = Path.Combine(Server.MapPath("~/Files"), fileName);

            if (System.IO.File.Exists(fullPath))
            {
                return File(fullPath, GetMimeType(fullPath));
            }
            else
            {
                TempData["Message"] = "File not found.";
                return RedirectToAction("Index");
            }
        }
        // Fungsi GetMimeType akan mengembalikan jenis konten yang sesuai berdasarkan ekstensi file
        private string GetMimeType(string filePath)
        {
            string mimeType = "application/unknown";
            string ext = Path.GetExtension(filePath).ToLower();
            Microsoft.Win32.RegistryKey regKey = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(ext);
            if (regKey != null && regKey.GetValue("Content Type") != null)
            {
                mimeType = regKey.GetValue("Content Type").ToString();
            }
            return mimeType;
        }

        public ActionResult VerifyPassword(string password)
        {
            // Ganti "1@23ANd" dengan sandi yang benar
            bool isPasswordValid = (password == "m@Ld1ngC3nt3R!823");

            return Json(new { success = isPasswordValid }, JsonRequestBehavior.AllowGet);
        }

        private string GetFileTypeByExtension(string fileExtension)
        {
            switch (fileExtension.ToLower())
            {
                case ".apk":
                    return "Application (Danger!)";
                case ".pdf":
                    return "Pdf ";
                case ".docx":
                case ".doc":
                    return " Word ";
                case ".xlsx":
                case ".xls":
                    return " Excel ";
                case ".txt":
                    return "Text ";
                case ".jpg":
                case ".jpeg":
                case ".png":
                    return "Image";
                case ".mp4":
                case ".webm":
                    return "Video";
                default:
                    return "Unknown";
            }
        }

    }
}

public class ObjFile
{
    public IEnumerable<HttpPostedFileBase> files { get; set; }
    public string File { get; set; }
    public double Size { get; set; }
    public string Type { get; set; }
}