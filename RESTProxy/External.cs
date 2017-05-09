using System;
using System.Runtime.InteropServices;

namespace RESTProxy
{
    class External
    {
        /// <summary>
        /// 
        /// Provides an interfact to Win32 (unmanaged code) apis for logging in to a Windows user account
        /// 
        /// </summary>
        /// <param name="lpszUsername"></param>
        /// <param name="lpszDomain"></param>
        /// <param name="lpszPassword"></param>
        /// <param name="dwLogonType"></param>
        /// <param name="dwLogonProvider"></param>
        /// <param name="phToken"></param>
        /// <returns></returns>
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool LogonUser(String lpszUsername, String lpszDomain, String lpszPassword,
        int dwLogonType, int dwLogonProvider, out IntPtr phToken);



    }

    //enum definitions taken from here:
    //https://sourceforge.net/p/mingw/mingw-org-wsl/ci/master/tree/include/winbase.h
    //https://msdn.microsoft.com/en-us/library/windows/desktop/aa378184%28v=vs.85%29.aspx?f=255&MSPPError=-2147217396
    //https://platinumdogs.me/2008/10/30/net-c-impersonation-with-network-credentials/
    public enum LogonType
    {
        LOGON32_LOGON_INTERACTIVE = 2,
        LOGON32_LOGON_NETWORK = 3,
        LOGON32_LOGON_BATCH = 4,
        LOGON32_LOGON_SERVICE = 5,
        LOGON32_LOGON_UNLOCK = 7,
        LOGON32_LOGON_NETWORK_CLEARTEXT = 8, // Win2K or higher
        LOGON32_LOGON_NEW_CREDENTIALS = 9 // Win2K or higher
    }

    public enum LogonProvider
    {
        LOGON32_PROVIDER_DEFAULT = 0,
        LOGON32_PROVIDER_WINNT35 = 1,
        LOGON32_PROVIDER_WINNT40 = 2,
        LOGON32_PROVIDER_WINNT50 = 3
    }

}
