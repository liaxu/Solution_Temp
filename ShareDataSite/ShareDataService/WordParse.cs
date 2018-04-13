using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace ShareDataService
{
    public class WordParse : WriteToFile, IParseFile
    {
        private XNamespace w = wordmlNamespace;

        public WordParse(byte[] data, string accessToken, string fileId)
        {
            base.ParsetempDataArray = this.ReadFileFromDownloadUriToStream(data);
            base.AccessToken = accessToken;
            base.FileId = fileId;
        }

        public TempData[] ReadFileFromDownloadUriToStream(byte[] data)
        {
            try
            {
                using (WordprocessingDocument wordprocessingDocument =
                 WordprocessingDocument.Open(new MemoryStream(data), false))
                {
                    List<TempData> result = new List<TempData>();

                    XDocument xDoc = null;
                    var wdPackage = wordprocessingDocument.Package;
                    PackageRelationship docPackageRelationship =
                            wdPackage
                            .GetRelationshipsByType(documentRelationshipType)
                            .FirstOrDefault();
                    if (docPackageRelationship != null)
                    {
                        Uri documentUri =
                            PackUriHelper
                            .ResolvePartUri(
                                new Uri("/", UriKind.Relative),
                                        docPackageRelationship.TargetUri);
                        PackagePart documentPart = wdPackage.GetPart(documentUri);
                        xDoc = XDocument.Load(XmlReader.Create(documentPart.GetStream()));
                    }

                    // Find all paragraphs in the document.  
                    var paragraphs =
                        from para in xDoc
                                     .Root
                                     .Element(w + "body")
                                     .Descendants(w + "p")
                                     where !para.Parent.Name.LocalName.Equals("tc")
                        select new
                        {
                            ParagraphNode = para
                        };

                    // Retrieve the text of each paragraph.  
                    var paraWithText =
                        from para in paragraphs
                        select ParagraphText(para.ParagraphNode);

                    result.AddRange(TempData.GetTempDataIEnumerable(StorageType.TextType, paraWithText));


                    // Find all tables in the document.
                    var tables =
                        wordprocessingDocument.MainDocumentPart.Document.Body.Elements<Table>();

                    // Retrieve the text of each table.  

                    var tablesText =tables.Select(table =>
                    {
                        var rows=table.Elements<TableRow>();
                        var rowsText=rows.Select(row =>
                        {
                            var cells=row.Elements<TableCell>();
                            var cellsText = cells.Select(cell =>
                            {
                                // Find the first paragraph in the table cell.
                                Paragraph p = cell.Elements<Paragraph>().FirstOrDefault();
                                // Find the first run in the paragraph.
                                Run r = p.Elements<Run>().FirstOrDefault();
                                if (r == null)
                                {
                                    return "<td></td>";
                                }
                                // Set the text for the run.
                                Text t = r.Elements<Text>().FirstOrDefault();
                                var text = (t == null ? "" : t.Text);
                                return "<td>" + text + "</td>";
                            });
                            return "<tr>" + string.Join("",cellsText) + "</tr>";
                        });
                        if (rowsText!=null && rowsText.Count()>0)
                        {
                            return string.Join("",rowsText);
                        }
                        return "";
                    });
                    
                    result.AddRange(TempData.GetTempDataIEnumerable(StorageType.TableType, tablesText));

                    // Find image and add to the result
                    var imageParts = wordprocessingDocument.MainDocumentPart.ImageParts;
                    byte[] arr = null;
                    foreach (ImagePart item in imageParts)
                    {
                        var stream = item.GetStream();
                        arr = new byte[stream.Length];
                        stream.Read(arr, 0, (int)stream.Length);
                        result.Add(new TempData { StorageType = StorageType.ImageType, Data = Convert.ToBase64String(arr) });
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
