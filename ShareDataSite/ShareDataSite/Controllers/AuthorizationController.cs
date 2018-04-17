using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Web;
using System.Web.Mvc;
using ShareDataSite.Filters;

namespace ShareDataSite.Controllers
{
    [AuthorizedViewData]
    public class AuthorizationController : Controller
    {

        [Route("Authorization/Login")]
        public ActionResult Login()
        {
            return View();
        }
        [Route("Authorization/Logout")]
        public ActionResult Logout()
        {
            return View();
        }
        [Route("Authorization/Authorize")]
        public ActionResult Authorize()
        {
            return View();
        }

        [Route("Authorization/Code")]
        public ActionResult Code(string code)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(new Uri(AuthorizedViewDataAttribute.token_url));
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            NameValueCollection outgoingQueryString = HttpUtility.ParseQueryString(String.Empty);
            outgoingQueryString.Add("code", code);
            outgoingQueryString.Add("client_id", AuthorizedViewDataAttribute.client_id);
            outgoingQueryString.Add("client_secret", AuthorizedViewDataAttribute.client_secret);
            outgoingQueryString.Add("redirect_uri", AuthorizedViewDataAttribute.redirect_uri);
            outgoingQueryString.Add("grant_type", "authorization_code");
            outgoingQueryString.Add("scope", AuthorizedViewDataAttribute.scope);
            string postdata = outgoingQueryString.ToString();
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(postdata);
            request.ContentLength = buffer.Length;
            Stream writer = request.GetRequestStream();
            writer.Write(buffer, 0, buffer.Length);
            writer.Close();
            try
            {
                var response = request.GetResponse();
                StreamReader sr = new StreamReader(response.GetResponseStream());
                string result = sr.ReadToEnd();
                return Content(result, "application/json");
            }
            catch (Exception ex)
            {
                if (ex is WebException webex)
                {
                    StreamReader sr = new StreamReader(webex.Response.GetResponseStream());
                    var a = sr.ReadToEnd();
                }
                throw;
            }
        }

        [Route("Authorization/RefreshToken")]
        public ActionResult RefreshToken(string refresh_token)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(new Uri(AuthorizedViewDataAttribute.token_url));
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            NameValueCollection outgoingQueryString = HttpUtility.ParseQueryString(String.Empty);
            outgoingQueryString.Add("client_id", AuthorizedViewDataAttribute.client_id);
            outgoingQueryString.Add("refresh_token", refresh_token);
            outgoingQueryString.Add("scope", AuthorizedViewDataAttribute.scope);
            outgoingQueryString.Add("redirect_uri", AuthorizedViewDataAttribute.redirect_uri);
            outgoingQueryString.Add("grant_type", "refresh_token");
            outgoingQueryString.Add("client_secret", AuthorizedViewDataAttribute.client_secret);
            string postdata = outgoingQueryString.ToString();
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(postdata);
            request.ContentLength = buffer.Length;
            Stream writer = request.GetRequestStream();
            writer.Write(buffer, 0, buffer.Length);
            writer.Close();
            try
            {
                var response = request.GetResponse();
                StreamReader sr = new StreamReader(response.GetResponseStream());
                string result = sr.ReadToEnd();
                return Content(result, "application/json");
            }
            catch (Exception ex)
            {
                if (ex is WebException webex)
                {
                    StreamReader sr = new StreamReader(webex.Response.GetResponseStream());
                    var a = sr.ReadToEnd();
                }
                throw;
            }
        }
    }
}