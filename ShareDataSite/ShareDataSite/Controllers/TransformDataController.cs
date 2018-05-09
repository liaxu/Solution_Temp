using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Mvc;
using Newtonsoft.Json;
using ShareDataService;
using ShareDataSite.Models;

namespace ShareDataSite.Controllers
{
    /// <summary>
    /// Convert files to RawData controller.
    /// </summary>
    public class TransformDataController : Controller
    {
        /// <summary>
        /// Get the converted raw data of the file and return it as html.
        /// </summary>
        /// <returns>Html with raw data.</returns>
        [Route("api/getrawdata")]
        [HttpPost]
        public string GetHtmlAfterTransformData()
        {
            try
            {

                // Get Onedrive file download address.
                Stream req = Request.InputStream;
                req.Seek(0, SeekOrigin.Begin);
                string json = new StreamReader(req).ReadToEnd();
                InputObject inputOjbect = null;
                try
                {
                    inputOjbect = JsonConvert.DeserializeObject<InputObject>(json);
                }
                catch (Exception)
                {
                    return string.Empty;
                }

                string downloadUri = inputOjbect.Downloaduri;
                string accessToken = inputOjbect.AccessToken;
                string fileId = inputOjbect.FileId;

                // Download the file in Onedrive.
                var webclient = new WebClient();
                byte[] data = webclient.DownloadData(downloadUri);

                // Get the file name.
                var fileName = webclient.ResponseHeaders.GetValues("Content-Disposition").FirstOrDefault();
                fileName = fileName.Replace("\"", "");
                var parse = new WriteRawDataToFile();
                if (fileName.ToLower().EndsWith(".doc") || fileName.ToLower().EndsWith(".docx"))
                {
                    parse = new WordParse(data, accessToken, fileId);
                }
                else if (fileName.ToLower().EndsWith(".xls") || fileName.ToLower().EndsWith(".xlsx"))
                {
                    parse = new ExcelParse(data, accessToken, fileId);
                }
                else if (fileName.ToLower().EndsWith(".ppt") || fileName.ToLower().EndsWith(".pptx"))
                {
                    parse = new PowerPointParse(data, accessToken, fileId);
                }
                else
                {
                    return string.Empty;
                }

                return parse.TempDataToHtmlAndUploadToOneDrive();
            }
            catch (Exception x)
            {
                Response.StatusCode = 400;
                return x.Message;
            }

        }

        /// <summary>
        /// Initialize the file list to Onedrive.
        /// </summary>
        /// <returns></returns>
        [Route("api/initFilesToOneDrive")]
        [HttpPost]
        public async Task InitFileListToOneDriveAsync(string accessToken)
        {
            try
            {
                var srcDir = Server.MapPath("~/App_Data");
                DirectoryInfo folder = new DirectoryInfo(srcDir);
                foreach (FileInfo file in folder.GetFiles())
                {
                    // Get stream objects from fileInfo object.
                    var fileStream = file.Open(FileMode.Open);

                    // Judgment file stream content length greater than 4MB use UploadBigFileToOneDrive method, otherwise use UploadSmallFileToOneDrive method.
                    if (new byte[fileStream.Length].Length < (4 * 1024 * 1024))
                    {
                        await UploadFileToOneDrive.UploadSmallFileToOneDrive(accessToken, fileStream, file.Name, "https://graph.microsoft.com/v1.0");
                    }
                    else
                    {
                        await UploadFileToOneDrive.UploadBigFileToOneDrive(accessToken, fileStream, file.Name, "https://graph.microsoft.com/v1.0");
                    }
                    fileStream.Close();
                }
            }
            catch (Exception x)
            {
                Response.StatusCode = 400;
            }
        }


    }
}