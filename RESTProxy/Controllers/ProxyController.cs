using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace RESTProxy.Controllers
{
    public class ProxyController : ApiController
    {
        [HttpPost]
        public HttpResponseMessage Authenticate(UserPass up)
        {
            //do auth stuff
            string username = up.Username;
            string password = up.Password;
            string domain = ConfigurationManager.AppSettings.Get("serviceaccount.domain");

            WindowsIdentity userAcct = AuthHelper.Login(username, password, domain);
            string resource = "authenticate";
            string httpVerb = "POST";
            
            //do passthru stuff
            var res = Request.CreateResponse(HttpStatusCode.OK);

            NameValueCollection outgoingQueryString = HttpUtility.ParseQueryString(String.Empty);
            outgoingQueryString.Add("username", username);
            outgoingQueryString.Add("password", password);
            string postdata = outgoingQueryString.ToString();

            //this method builds up the request with the authentication token
            HttpWebRequest request = GetAuthenticatedRequest(userAcct, resource, httpVerb, Request, postdata);

            res.Content = new StreamContent(request.GetResponse().GetResponseStream());
            return res;
        }


        [AcceptVerbs("GET","POST")]
        public async Task<HttpResponseMessage> Passthru(string resource)
        {
            WindowsIdentity userAcct = AuthHelper.GetServiceAcct();
            string httpVerb = Request.Method.ToString();
            
            string rawData = await Request.Content.ReadAsStringAsync();

            HttpWebRequest request = GetAuthenticatedRequest(userAcct, resource, httpVerb, Request, rawData);
            
            //do passthru stuff
            var res = Request.CreateResponse(HttpStatusCode.OK);
            res.Content = new StreamContent(request.GetResponse().GetResponseStream());
            return res;
        }

        private HttpWebRequest GetAuthenticatedRequest(WindowsIdentity serviceAcct, string resource, string httpVerb, 
                                                       HttpRequestMessage origRequest, string payload)
        {
            string url = ConfigurationManager.AppSettings.Get("outbound.endpoint") + resource;
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
            using (serviceAcct.Impersonate())
            {
                request.Method = httpVerb;
                request.UseDefaultCredentials = true;
                request.PreAuthenticate = true;
                request.Credentials = CredentialCache.DefaultNetworkCredentials;
            }

            reassignHeaders(request, origRequest.Headers);

            //if request is a post, attach the message body
            if (httpVerb.ToUpper().Equals("POST"))
            {
                using (var stream = request.GetRequestStream())
                {
                    using (var writer = new StreamWriter(stream))
                    {
                        writer.Write(payload);
                        writer.Flush();
                    }
                }

                request.ContentType = Request.Content.Headers.ContentType.ToString();
            }

            return request;
        }

        private void reassignHeaders(HttpWebRequest request, HttpRequestHeaders headers)
        {
            var hdict = headers.ToDictionary(kvp => kvp.Key, kvp => String.Join(", ", kvp.Value));

            //set everything except "Connection"
            //any headers that aren't defined on "request" add to header collection directly
            var skipped = hdict.Aggregate<KeyValuePair<string, string>, List<string>>(
                new List<string>(),
                (last, kv) => {

                    if (!kv.Key.Equals("Connection"))
                    {
                        string headerKey = kv.Key;
                        PropertyInfo prop = request.GetType().GetProperty(headerKey.Replace("-", ""));
                        if (prop != null)
                        {
                            prop.SetValue(request, kv.Value);
                        }
                        else
                        {
                            request.Headers.Set(headerKey, kv.Value);
                        }
                    }
                    return last;
                });


            string connHeader = Request.Headers.Connection.FirstOrDefault();
            if (connHeader.ToUpper().Equals("KEEP-ALIVE"))
            {
                request.KeepAlive = true;
            }
            else if (connHeader.ToUpper().Equals("CLOSE"))
            {
                request.KeepAlive = false;
            }
            else
            {
                request.Connection = connHeader;
            }
        }
    }

    public class UserPass
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }


}
