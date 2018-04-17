using System.Web.Mvc;
using ShareDataSite.Filters;

namespace ShareDataSite.Controllers
{
    [AuthorizedViewData]
    public class HomeController : Controller
    {
        [Route("")]
        public ActionResult Index()
        {
            return View("filelist");
        }

        [Route("rawlist")]
        public ActionResult RawList()
        {
            return View("rawlist");
        }

        [Route("rawdata")]
        public ActionResult RawData()
        {
            return View("rawdata");
        }

        [Route("test")]
        public ActionResult test(string view)
        {
            return View(view);
        }

        public ActionResult PageNotFound()
        {
            return View("pagenotfound");
        }
    }
}