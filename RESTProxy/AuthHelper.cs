using System;
using System.Configuration;
using System.Diagnostics;
using System.Runtime.Caching;
using System.Security.Principal;

namespace RESTProxy
{
    class AuthHelper
    {
        /// <summary>
        /// 
        /// Accecpts a username, password, and domain, and calls the Logon api (Win32) to log
        /// the user in and return a WindowsIdentity object.
        /// 
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="domain"></param>
        /// <returns>WindowsIdentity object for the passed in credentials</returns>
        internal static WindowsIdentity Login(string username, string password, string domain)
        {
            //get default instance of MemoryCache
            ObjectCache cache = MemoryCache.Default;

            //use + and () as delineator: 
            // + is an invalid char in usernames
            // () are invalid in domain names
            string key = String.Format("(%s)+(%s)+(%s)", username, password, domain);

            //attempts to load the contents of the "filecontents" cache item
            WindowsIdentity serviceAcct = cache[key] as WindowsIdentity;

            //if this value is null, then the windows identity for the provided credentials was not found in cache
            //most of this code is found here http://stackoverflow.com/questions/9909784/impersonating-a-windows-user
            if (serviceAcct == null)
            {

                IntPtr userToken = IntPtr.Zero;

                bool success = External.LogonUser(
                  username,
                  domain,
                  password,
                  (int)LogonType.LOGON32_LOGON_INTERACTIVE, //2
                  (int)LogonProvider.LOGON32_PROVIDER_DEFAULT, //0
                  out userToken);

                serviceAcct = new WindowsIdentity(userToken);

                //create a caching policy object with configured duration in minutes
                CacheItemPolicy policy = new CacheItemPolicy();
                double minutes = 1.0;
                Double.TryParse(ConfigurationManager.AppSettings.Get("cachepolicy.expiration.minutes"), out minutes);
                policy.AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(minutes);
                cache.Set(key, serviceAcct, policy);
            }
            return serviceAcct;
        }


        /// <summary>
        /// Pulls the service account username, password, and domain from the
        /// config file and passes them to the Login method. Returns the WindowsIdentity
        /// created by Login()
        /// 
        /// </summary>
        /// <returns>WindowsIdentity object for the configured service account</returns>
        internal static WindowsIdentity GetServiceAcct()
        {
            string username = ConfigurationManager.AppSettings.Get("serviceaccount.username");
            string password = ConfigurationManager.AppSettings.Get("serviceaccount.password");
            string domain = ConfigurationManager.AppSettings.Get("serviceaccount.domain");
            return Login(username, password, domain);
        }
    }
}
