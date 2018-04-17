using System.Web.Mvc;

namespace ShareDataSite.Filters
{
    public class AuthorizedViewDataAttribute : ActionFilterAttribute
    {
        public const string auth_url = "https://login.microsoftonline.com/common/oauth2/v2.0/authorize";
        public const string token_url = "https://login.microsoftonline.com/common/oauth2/v2.0/token";
        public const string logout_url = "";         // recommended if available
        public const string client_id = "e0375f87-c47c-4180-9e20-ed3cebd53353";          // required
        public const string scope = "offline_access openid User.Read Files.Read.All Files.ReadWrite.All Sites.Read.All Sites.ReadWrite.All";
        public const string response_type = "token";
        public const string redirect_uri = "https://localhost:44313/Authorization/Authorize";

        public const string client_secret = "xvqmxVWR403=(crCZGQ93=!";
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            filterContext.Controller.ViewBag.auth_url = auth_url;
            filterContext.Controller.ViewBag.token_url = token_url;
            filterContext.Controller.ViewBag.logout_url = logout_url;
            filterContext.Controller.ViewBag.client_id = client_id;
            filterContext.Controller.ViewBag.scope = scope;
            filterContext.Controller.ViewBag.response_type = response_type;
            filterContext.Controller.ViewBag.redirect_uri = redirect_uri;
            base.OnActionExecuting(filterContext);
        }
    }
}