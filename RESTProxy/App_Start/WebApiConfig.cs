using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Web.Http;
using System.Web.Http.Routing;

namespace RESTProxy
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API configuration and services
            ServicePointManager.ServerCertificateValidationCallback =
               delegate (object sender, X509Certificate certificate, X509Chain chain,
                   SslPolicyErrors sslPolicyErrors) { return true; };


            // Web API routes
            config.MapHttpAttributeRoutes();

            //POST requests to "authenticate" go to the authenticate controller action
            config.Routes.MapHttpRoute(
                name: "AuthIntercept",
                routeTemplate: "api/authenticate",
                defaults: new { controller = "Proxy", action = "Authenticate" },
                constraints: new { httpMethod = new HttpMethodConstraint(HttpMethod.Post) }
            );

            //everything else gets passed through
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{*resource}",
                defaults: new { controller = "Proxy", action = "Passthru" }
            );
        }
    }
}
