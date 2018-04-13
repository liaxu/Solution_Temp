using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShareDataService
{
    public class ExcelParse : WriteToFile, IParseFile
    {
        public ExcelParse(byte[] data, string accessToken, string fileId)
        {
            base.ParsetempDataArray = this.ReadFileFromDownloadUriToStream(data);
            base.AccessToken = accessToken;
            base.FileId = fileId;
        }

        public TempData[] ReadFileFromDownloadUriToStream(byte[] data)
        {
            try
            {
                using (SpreadsheetDocument spreadsheetDocument =
                 SpreadsheetDocument.Open(new MemoryStream(data), false))
                {

                    IEnumerable<Row> rows = null;
                    string[] cellTexts = null;
                    IEnumerable<string> rowTexts = null;
                    Stream stream = null;
                    byte[] streamByteArr = null;
                    List<TempData> result = new List<TempData>();
                    WorkbookPart wbPart = spreadsheetDocument.WorkbookPart;
                    var sheets = wbPart.Workbook.Descendants<Sheet>();
                    foreach (var sheet in sheets)
                    {
                        WorksheetPart wsPart =
                       (WorksheetPart)(wbPart.GetPartById(sheet.Id));
                        rows = wsPart.Worksheet.Descendants<Row>();
                        rowTexts = rows.Select(m =>
                        {
                            cellTexts = m.Descendants<Cell>().Select(cell =>
                            {
                                var str = GetCellText(cell, wbPart);
                                return "<td>" + str + "</td>";
                            }).ToArray();
                            return "<tr>" + string.Join("", cellTexts) + "</tr>";
                        });
                        result.Add(new TempData { StorageType = StorageType.TableType, Data = string.Join("", rowTexts) });

                        if (wsPart.DrawingsPart != null && wsPart.DrawingsPart.ImageParts != null)
                        {
                            var imgs = wsPart.DrawingsPart.ImageParts.Select(m =>
                            {
                                stream = m.GetStream();
                                streamByteArr = new byte[stream.Length];
                                stream.Read(streamByteArr, 0, (int)stream.Length);
                                return new TempData { StorageType = StorageType.ImageType, Data = Convert.ToBase64String(streamByteArr) };
                            }).ToArray();
                            result.AddRange(imgs);
                        }
                    }
                    return result.ToArray();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
