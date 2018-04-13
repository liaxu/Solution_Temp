using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Xml.Linq;

namespace ShareDataService
{
    public class PowerPointParse : WriteToFile, IParseFile
    {
        public PowerPointParse(byte[] data, string accessToken, string fileId)
        {
            base.ParsetempDataArray = this.ReadFileFromDownloadUriToStream(data);
            base.AccessToken = accessToken;
            base.FileId = fileId;
        }

        public TempData[] ReadFileFromDownloadUriToStream(byte[] data)
        {
            try
            {
                using (PresentationDocument presentationDocument =
                 PresentationDocument.Open(new MemoryStream(data), false))
                {
                    List<TempData> result = new List<TempData>();

                    PresentationPart part = presentationDocument.PresentationPart;
                    OpenXmlElementList childElements = part.Presentation.SlideIdList.ChildElements;

                    var slideParts = from item in childElements
                                     select (SlidePart)part.GetPartById((item as SlideId).RelationshipId);

                    var slideText = from item in slideParts
                                    select (WriteToFile.GetSlideText(item));
                    result.AddRange(TempData.GetTempDataIEnumerable(StorageType.TextType, slideText));

                    Stream stream = null;
                    byte[] streamByteArr = null;
                    foreach (var slide in slideParts)
                    {
                        result.AddRange(slide.ImageParts.Select(m =>
                        {
                            stream = m.GetStream();
                            streamByteArr = new byte[stream.Length];
                            stream.Read(streamByteArr, 0, (int)stream.Length);
                            return new TempData { StorageType = StorageType.ImageType, Data = Convert.ToBase64String(streamByteArr) };
                        }).ToArray());
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
