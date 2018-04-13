using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShareDataService
{
    public class TempData
    {
        public StorageType StorageType { get; set; }
        public string Data { get; set; }
        static public IEnumerable<TempData> GetTempDataIEnumerable(StorageType storageType,IEnumerable<string> dataList)
        {
            foreach (var data in dataList)
            {
                yield return new TempData { StorageType = storageType, Data = data };
            }
        }
    }
}
