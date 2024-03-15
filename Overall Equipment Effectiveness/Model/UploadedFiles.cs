using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OEE_SSC.Models
{
    public class UploadedFiles
    {
        public string FileN { get; set; }
        public string FilePath { get; set; }
        public HttpPostedFileBase UploadFile { get; set; }
        public List<UploadedFiles> lstUploadedFiles { get; set; }
    }
}