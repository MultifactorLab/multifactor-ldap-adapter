using System.Configuration;
using System;
using System.Net;
using MultiFactor.Ldap.Adapter.Core;

namespace MultiFactor.Ldap.Adapter.Configuration
{
    public class LdapServerConfig : ILdapServerConfig
    {
        public bool IsEmpty => AdapterLdapEndpoint == null && AdapterLdapsEndpoint == null;

        public IPEndPoint AdapterLdapEndpoint { get; }
        public IPEndPoint AdapterLdapsEndpoint { get; }

        private LdapServerConfig(IPEndPoint adapterLdapEndpoint, IPEndPoint adapterLdapsEndpoint)
        {
            AdapterLdapEndpoint = adapterLdapEndpoint;
            AdapterLdapsEndpoint = adapterLdapsEndpoint;
        }

        public static LdapServerConfig Parse(AppSettingsSection appSettings)
        {
            if (appSettings is null) throw new ArgumentNullException(nameof(appSettings));

            IPEndPoint adapterLdapEndpoint = null;
            IPEndPoint adapterLdapsEndpoint = null;

            var ldapEndpointSetting = appSettings.Settings[Constants.Configuration.AdapterLdapEndpoint]?.Value;
            if (!string.IsNullOrEmpty(ldapEndpointSetting))
            {
                if (!TryParseIPEndPoint(ldapEndpointSetting, out var ldapEndpoint))
                {
                    throw new Exception($"Configuration error: Can't parse '{Constants.Configuration.AdapterLdapEndpoint}' value");
                }
                adapterLdapEndpoint = ldapEndpoint;
            }

            var ldapsEndpointSetting = appSettings.Settings[Constants.Configuration.AdapterLdapsEndpoint]?.Value;
            if (!string.IsNullOrEmpty(ldapsEndpointSetting))
            {
                if (!TryParseIPEndPoint(ldapsEndpointSetting, out var ldapsEndpoint))
                {
                    throw new Exception($"Configuration error: Can't parse '{Constants.Configuration.AdapterLdapsEndpoint}' value");
                }
                adapterLdapsEndpoint = ldapsEndpoint;
            }

            return new LdapServerConfig(adapterLdapEndpoint, adapterLdapsEndpoint);
        }

        private static bool TryParseIPEndPoint(string text, out IPEndPoint ipEndPoint)
        {
            Uri uri;
            ipEndPoint = null;

            if (Uri.TryCreate(string.Concat("tcp://", text), UriKind.Absolute, out uri))
            {
                ipEndPoint = new IPEndPoint(IPAddress.Parse(uri.Host), uri.Port < 0 ? 0 : uri.Port);
                return true;
            }
            if (Uri.TryCreate(string.Concat("tcp://", string.Concat("[", text, "]")), UriKind.Absolute, out uri))
            {
                ipEndPoint = new IPEndPoint(IPAddress.Parse(uri.Host), uri.Port < 0 ? 0 : uri.Port);
                return true;
            }

            throw new FormatException($"Failed to parse {text} to IPEndPoint");
        }
    }
}
