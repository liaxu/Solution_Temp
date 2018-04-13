using Newtonsoft.Json;
using ShareDataService;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace ShareDataSite.Controllers
{
    public class TransformDataController : Controller
    {
        public class InputObject
        {
            public string AccessToken { get; set; }
            public string FileId { get; set; }
        }
        [Route("api/getrawdata")]
        [HttpPost]
        public string GetHtmlAfterTransformData()
        {
            string downloadUri = this.Request.QueryString["downloaduri"];

            Stream req = Request.InputStream;
            req.Seek(0, System.IO.SeekOrigin.Begin);
            string json = new StreamReader(req).ReadToEnd();

            InputObject inputOjbect = null;
            try
            {
                inputOjbect = JsonConvert.DeserializeObject<InputObject>(json);
            }
            catch (Exception)
            {
                return String.Empty;
            }

            string accessToken = inputOjbect.AccessToken;
            string fileId = inputOjbect.FileId;

            var webclient = new WebClient();
            byte[] data = webclient.DownloadData(downloadUri);
            var fileName = webclient.ResponseHeaders.GetValues("Content-Disposition").FirstOrDefault();

            var parse = new WriteToFile();
            if (fileName.ToLower().Contains(".doc") || fileName.ToLower().Contains(".docx"))
            {
                parse = new WordParse(data, accessToken, fileId);
            }
            else if (fileName.ToLower().Contains(".xls") || fileName.ToLower().Contains(".xlsx"))
            {
                parse = new ExcelParse(data, accessToken, fileId);
            }
            else if (fileName.ToLower().Contains(".ppt") || fileName.ToLower().Contains(".pptx"))
            {
                parse = new PowerPointParse(data, accessToken, fileId);
            }
            else
            {
                return String.Empty;
            }

            return parse.TempDataToHtml();
        }
    }
}