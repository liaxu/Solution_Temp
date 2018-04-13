using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Graph;

namespace ShareDataService
{
    public class WriteToFile
    {
        public delegate Task UploadFile(string accessToken, Stream file, string fileName);
        public UploadFile UploadFileMethod { get; set; }
        public string AccessToken { get; set; }
        public string FileId { get; set; }

        public const string documentRelationshipType =
          "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument";
        public const string stylesRelationshipType =
          "http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles";
        public const string wordmlNamespace =
          "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        public const string endpointBase = @"https://graph.microsoft.com/v1.0";
        public const string rawDataPath = @"/SharedDataApp/RawData/";

        public static XNamespace powerpointmlNamespace = "http://schemas.openxmlformats.org/drawingml/2006/main";

        public TempData[] ParsetempDataArray { get; set; }

        public static string GetSlideText(SlidePart slidePart)
        {
            XDocument xDoc = XDocument.Load(XmlReader.Create(slidePart.GetStream()));
            if (xDoc == null)
            {
                return "";
            }
            return string.Join("", xDoc.Root.Descendants(powerpointmlNamespace + "t").Select(m => (string)m));
        }

        public static string GetCellText(Cell cell, WorkbookPart wbPart)
        {
            var value = cell.InnerText;
            if (cell.DataType != null)
                switch (cell.DataType.Value)
                {
                    case CellValues.SharedString:

                        var stringTable =
                           wbPart.GetPartsOfType<SharedStringTablePart>()
                           .FirstOrDefault();

                        if (stringTable != null)
                        {
                            value =
                               stringTable.SharedStringTable
                               .ElementAt(int.Parse(value)).InnerText;
                        }
                        break;

                    case CellValues.Boolean:
                        switch (value)
                        {
                            case "0":
                                value = "FALSE";
                                break;
                            default:
                                value = "TRUE";
                                break;
                        }
                        break;
                };
            return value;
        }

        public static string ParagraphText(XElement e)
        {
            XNamespace w = e.Name.Namespace;

            return e
                   .Descendants(w + "t")
                   .StringConcatenate(element => (string)element);
        }

        public static Stream GenerateStreamFromString(string s)
        {
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        virtual public string TempDataToHtml()
        {
            var result = string.Empty;
            foreach (var tempData in this.ParsetempDataArray)
            {
                switch (tempData.StorageType)
                {
                    case StorageType.TextType:
                        //for the brower can display xml snippet normally
                        tempData.Data = tempData.Data.Replace("<", @"&lt;");
                        result += "<div class=\"base text\">" + tempData.Data + @"</div>";
                        break;
                    case StorageType.ImageType:
                        result += "<div class=\"base image\"><img src=\"data:image/png;base64, " + tempData.Data + "\"/></div>";
                        break;
                    case StorageType.TableType:
                        result += "<div class=\"table-responsive\"><button style=\"display:none\">Insert Table</button><table class=\"table\"><tbody>" + tempData.Data + @"</tbody></table></div>";
                        break;
                    default:
                        break;
                }
            }

            var fileStream = GenerateStreamFromString(result);
            if (new byte[fileStream.Length].Length < (4 * 1024 * 1024))
            {
                this.UploadFileMethod = UploadSmallFile;
            }
            else
            {
                this.UploadFileMethod = UploadBigFile;
            }

            var fileName = rawDataPath;

            try
            {
                GraphServiceClient graphServiceClient = new GraphServiceClient(endpointBase,
                        new DelegateAuthenticationProvider(
                            async (requestMessage) =>
                            {
                                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("bearer", this.AccessToken);
                            }));

                var itemResponse = graphServiceClient.Me.Drive.Items[FileId].Request().GetAsync();
                fileName += itemResponse.Result.Name + ".rawdata";
            }
            catch (Exception ex)
            {
                fileName += "noname.rawdata";
                throw ex;
            }

            UploadFileMethod(this.AccessToken, fileStream, fileName);
            return result;
        }

        public async Task UploadSmallFile(string accessToken, Stream file, string fileName)
        {
            string endpoint = string.Format("{0}/me/drive/root:/{1}:/content", endpointBase, fileName);
            using (var client = new HttpClient())
            {
                using (var request = new HttpRequestMessage(HttpMethod.Put, endpoint))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                    request.Content = new StreamContent(file);
                    request.Content.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
                    using (var response = await client.SendAsync(request))
                    {
                        if (!response.IsSuccessStatusCode)
                        {
                            throw new Exception("Upload to OneDrive Fail");
                        }
                    }
                }
            }
        }

        public async Task UploadBigFile(string accessToken, Stream file, string fileName)
        {
            GraphServiceClient graphServiceClient = new GraphServiceClient(endpointBase,
                        new DelegateAuthenticationProvider(
                            async (requestMessage) =>
                            {
                                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("bearer", accessToken);
                            }));

            var uploadSession = await graphServiceClient.Me.Drive.Root.ItemWithPath(fileName).CreateUploadSession().Request().PostAsync();
            var maxChunkSize = 320 * 1024; // 320 KB - Change this to your chunk size. 5MB is the default.
            var provider = new ChunkedUploadProvider(uploadSession, graphServiceClient, file, maxChunkSize);
            // Setup the chunk request necessities
            var chunkRequests = provider.GetUploadChunkRequests();
            var readBuffer = new byte[maxChunkSize];
            var trackedExceptions = new List<Exception>();
            DriveItem itemResult = null;
            //upload the chunks
            foreach (var request in chunkRequests)
            {
                // Send chunk request
                var result = await provider.GetChunkRequestResponseAsync(request, readBuffer, trackedExceptions);

                if (result.UploadSucceeded)
                {
                    itemResult = result.ItemResponse;
                }
            }

            if (itemResult == null)
            {
                throw new Exception("Upload to OneDirve Fail");
            }
        }
    }
}
