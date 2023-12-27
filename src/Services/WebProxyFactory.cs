using System.Net;
using System;

namespace MultiFactor.Ldap.Adapter.Services
{
    internal static class WebProxyFactory
    {
        public static bool TryCreateWebProxy(string proxyAddress, out WebProxy proxy)
        {
            if (!TryParseUri(proxyAddress, out var proxyUri))
            {
                proxy = null;
                return false;
            }

            proxy = new WebProxy(proxyUri);
            SetProxyCredentials(proxy, proxyUri);
            return true;
        }

        private static bool TryParseUri(string apiUri, out Uri uri)
        {
            if (Uri.TryCreate(apiUri, UriKind.Absolute, out uri))
            {
                return true;
            }
            var uriSeparatorIdx = apiUri.LastIndexOf('@');
            if (uriSeparatorIdx == -1)
            {
                return false;
            }

            var leftPart = apiUri.Substring(0, uriSeparatorIdx).Replace("@", "%40");
            var rightPart = apiUri.Substring(uriSeparatorIdx + 1);
            var escapedUri = $"{leftPart}@{rightPart}";
            uri = new Uri(escapedUri);
            return true;
        }

        private static void SetProxyCredentials(WebProxy proxy, Uri proxyUri)
        {
            if (string.IsNullOrEmpty(proxyUri.UserInfo))
            {
                return;
            }

            var credentials = proxyUri.UserInfo.Split(new[] { ':' }, 2);
            proxy.Credentials = new NetworkCredential(credentials[0], credentials[1]);
        }
    }
}
