//Copyright(c) 2021 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-ldap-adapter/blob/main/LICENSE.md

using System;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace MultiFactor.Ldap.Adapter
{
    /// <summary>
    /// Service configuration
    /// </summary>
    public class Configuration
    {
        public Configuration()
        {
            BypassSecondFactorWhenApiUnreachable = true; //by default
            ServiceAccounts = new string[0];
        }

        #region general settings

        /// <summary>
        /// LDAP server name or address
        /// </summary>
        public string LdapServer { get; set; }

        /// <summary>
        /// This service LDAP endpoint
        /// </summary>
        public IPEndPoint AdapterLdapEndpoint { get; set; }

        public bool StartLdapServer { get; set; }

        /// <summary>
        /// This service LDAPS endpoint
        /// </summary>
        public IPEndPoint AdapterLdapsEndpoint { get; set; }

        public bool StartLdapsServer { get; set; }

        /// <summary>
        /// LDAP Server endpoint
        /// </summary>
        public string RemoteEndpoint { get; set; }

        /// <summary>
        /// Bypass second factor when MultiFactor API is unreachable
        /// </summary>
        public bool BypassSecondFactorWhenApiUnreachable { get; set; }

        /// <summary>
        /// Service accounts list - bind requests from its will be ignored
        /// </summary>
        public string[] ServiceAccounts { get; set; }

        /// <summary>
        /// Service accounts OU - bind requests with this OU will be ignored
        /// </summary>
        public string[] ServiceAccountsOrganizationUnit { get; set; }
        
        #endregion

        #region API settings

        /// <summary>
        /// Multifactor API URL
        /// </summary>
        public string ApiUrl { get; set; }
        /// <summary>
        /// HTTP Proxy for API
        /// </summary>
        public string ApiProxy { get; set; }

        /// <summary>
        /// Multifactor API KEY
        /// </summary>
        public string NasIdentifier { get; set; }

        /// <summary>
        /// API Secret
        /// </summary>
        public string MultiFactorSharedSecret { get; set; }

        #endregion

        /// <summary>
        /// Logging level
        /// </summary>
        public string LogLevel { get; set; }

        /// <summary>
        /// Certificate for TLS
        /// </summary>
        public X509Certificate2 X509Certificate { get; set; }

        #region load config section

        /// <summary>
        /// Read and load settings from appSettings configuration section
        /// </summary>
        public static Configuration Load()
        {
            var appSettings = ConfigurationManager.AppSettings;

            var adapterLdapEndpointSetting = appSettings["adapter-ldap-endpoint"]; 
            var adapterLdapsEndpointSetting = appSettings["adapter-ldaps-endpoint"];

            var ldapServerSetting = appSettings["ldap-server"];
            var apiUrlSetting = appSettings["multifactor-api-url"];
            var apiProxySetting = appSettings["multifactor-api-proxy"];
            var bypassSecondFactorWhenApiUnreachableSetting = appSettings["bypass-second-factor-when-api-unreachable"];
            var nasIdentifierSetting = appSettings["multifactor-nas-identifier"];
            var multiFactorSharedSecretSetting = appSettings["multifactor-shared-secret"];
            var logLevelSetting = appSettings["logging-level"];

            var serviceAccountsSetting = appSettings["ldap-service-accounts"];
            var serviceAccountsOrganizationUnitSetting = appSettings["ldap-service-accounts-ou"];

            if (string.IsNullOrEmpty(ldapServerSetting))
            {
                throw new Exception("Configuration error: 'ldap-server' element not found");
            }

            if (string.IsNullOrEmpty(apiUrlSetting))
            {
                throw new Exception("Configuration error: 'multifactor-api-url' element not found");
            }
            if (string.IsNullOrEmpty(nasIdentifierSetting))
            {
                throw new Exception("Configuration error: 'multifactor-nas-identifier' element not found");
            }
            if (string.IsNullOrEmpty(multiFactorSharedSecretSetting))
            {
                throw new Exception("Configuration error: 'multifactor-shared-secret' element not found");
            }
            if (string.IsNullOrEmpty(logLevelSetting))
            {
                throw new Exception("Configuration error: 'logging-level' element not found");
            }


            var configuration = new Configuration
            {
                LdapServer = ldapServerSetting,
                ApiUrl = apiUrlSetting,
                ApiProxy = apiProxySetting,
                NasIdentifier = nasIdentifierSetting,
                MultiFactorSharedSecret = multiFactorSharedSecretSetting,
                LogLevel = logLevelSetting,
            };

            if (!string.IsNullOrEmpty(adapterLdapEndpointSetting))
            {
                if (!TryParseIPEndPoint(adapterLdapEndpointSetting, out var adapterLdapEndpoint))
                {
                    throw new Exception("Configuration error: Can't parse 'adapter-ldap-endpoint' value");
                }
                configuration.AdapterLdapEndpoint = adapterLdapEndpoint;
                configuration.StartLdapServer = true;
            }

            if (!string.IsNullOrEmpty(adapterLdapsEndpointSetting))
            {
                if (!TryParseIPEndPoint(adapterLdapsEndpointSetting, out var adapterLdapsEndpoint))
                {
                    throw new Exception("Configuration error: Can't parse 'adapter-ldaps-endpoint' value");
                }
                configuration.AdapterLdapsEndpoint = adapterLdapsEndpoint;
                configuration.StartLdapsServer = true;
            }

            if (!(configuration.StartLdapServer || configuration.StartLdapsServer))
            {
                throw new Exception("Configuration error: Neither 'adapter-ldap-endpoint' or 'adapter-ldaps-endpoint' configured");
            }

            if (!string.IsNullOrEmpty(serviceAccountsSetting))
            {
                configuration.ServiceAccounts = serviceAccountsSetting
                    .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(acc => acc.Trim().ToLower())
                    .ToArray();
            }

            if (!string.IsNullOrEmpty(serviceAccountsOrganizationUnitSetting))
            {
                configuration.ServiceAccountsOrganizationUnit = serviceAccountsOrganizationUnitSetting
                    .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(acc => acc.Trim().ToLower())
                    .ToArray();
            }

            if (bypassSecondFactorWhenApiUnreachableSetting != null)
            {
                if (bool.TryParse(bypassSecondFactorWhenApiUnreachableSetting, out var bypassSecondFactorWhenApiUnreachable))
                {
                    configuration.BypassSecondFactorWhenApiUnreachable = bypassSecondFactorWhenApiUnreachable;
                }
            }

            return configuration;
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

        #endregion

        #region static members

        public static string GetLogFormat()
        {
            var appSettings = ConfigurationManager.AppSettings;
            return appSettings?["logging-format"];
        }

        #endregion
    }
}
