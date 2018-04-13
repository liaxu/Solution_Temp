using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShareDataService
{
    public interface IParseFile
    {
        TempData[] ReadFileFromDownloadUriToStream(byte[] data);
    }
}
