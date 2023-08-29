using System.IO;
using System;

namespace MultiFactor.Ldap.Adapter.Core
{
    public static class Constants
    {
        public const int BYTES_IN_MB = 1024 * 1024;
        public static readonly string ApplicationPath = $"{Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory)}{Path.DirectorySeparatorChar}";

        public static class Configuration
        {
            public const string AdapterLdapEndpoint = "adapter-ldap-endpoint";
            public const string AdapterLdapsEndpoint = "adapter-ldaps-endpoint";
            public const string AuthenticationCacheLifetime = "authentication-cache-lifetime";

            public static class PciDss
            {
                public const string InvalidCredentialDelay = "invalid-credential-delay";
            }
        }
    }
}
