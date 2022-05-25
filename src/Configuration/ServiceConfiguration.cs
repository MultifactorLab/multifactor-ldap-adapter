//Copyright(c) 2021 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-ldap-adapter/blob/main/LICENSE.md

using NetTools;
using Serilog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace MultiFactor.Ldap.Adapter.Configuration
{
    /// <summary>
    /// Service configuration
    /// </summary>
    public class ServiceConfiguration
    {
        /// <summary>
        /// List of clients with identification by client ip
        /// </summary>
        private IDictionary<IPAddress, ClientConfiguration> _ipClients;

        public ServiceConfiguration()
        {
            _ipClients = new Dictionary<IPAddress, ClientConfiguration>();
        }

        private void AddClient(IPAddress ip, ClientConfiguration client)
        {
            if (_ipClients.ContainsKey(ip))
            {
                throw new ConfigurationErrorsException($"Client with IP {ip} already added from {_ipClients[ip].Name}.config");
            }
            _ipClients.Add(ip, client);
        }

        public ClientConfiguration GetClient(IPAddress ip)
        {
            if (SingleClientMode)
            {
                return _ipClients[IPAddress.Any];
            }
            if (_ipClients.ContainsKey(ip))
            {
                return _ipClients[ip];
            }
            return null;
        }

        #region general settings

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

        #endregion

        /// <summary>
        /// Logging level
        /// </summary>
        public string LogLevel { get; set; }

        /// <summary>
        /// Certificate for TLS
        /// </summary>
        public X509Certificate2 X509Certificate { get; set; }

        public bool SingleClientMode { get; set; }


        #region load config section

        /// <summary>
        /// Read and load settings from appSettings configuration section
        /// </summary>
        public static ServiceConfiguration Load(ILogger logger)
        {
            var serviceConfig = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            var appSettingsSection = serviceConfig.GetSection("appSettings");
            var appSettings = appSettingsSection as AppSettingsSection;


            var adapterLdapEndpointSetting      = appSettings.Settings["adapter-ldap-endpoint"]?.Value; 
            var adapterLdapsEndpointSetting     = appSettings.Settings["adapter-ldaps-endpoint"]?.Value;
            var apiUrlSetting                   = appSettings.Settings["multifactor-api-url"]?.Value;
            var apiProxySetting                 = appSettings.Settings["multifactor-api-proxy"]?.Value;
            var logLevelSetting                 = appSettings.Settings["logging-level"]?.Value;


            if (string.IsNullOrEmpty(apiUrlSetting))
            {
                throw new Exception("Configuration error: 'multifactor-api-url' element not found");
            }
            if (string.IsNullOrEmpty(logLevelSetting))
            {
                throw new Exception("Configuration error: 'logging-level' element not found");
            }

            var configuration = new ServiceConfiguration
            {
                ApiUrl = apiUrlSetting,
                ApiProxy = apiProxySetting,
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

            var clientConfigFilesPath = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory) + Path.DirectorySeparatorChar + "clients";
            var clientConfigFiles = Directory.Exists(clientConfigFilesPath) ? Directory.GetFiles(clientConfigFilesPath, "*.config") : new string[0];

            if (clientConfigFiles.Length == 0)
            {
                //check if we have anything
                var ldapServer = appSettings.Settings["ldap-server"]?.Value;
                if (ldapServer == null)
                {
                    throw new ConfigurationErrorsException("No clients' config files found. Use one of the *.template files in the /clients folder to customize settings. Then save this file as *.config.");
                }

                var client = Load("General", appSettings);
                configuration.AddClient(IPAddress.Any, client);
                configuration.SingleClientMode = true;
            }
            else
            {
                foreach (var clientConfigFile in clientConfigFiles)
                {
                    logger.Information($"Loading client configuration from {Path.GetFileName(clientConfigFile)}");

                    var customConfigFileMap = new ExeConfigurationFileMap();
                    customConfigFileMap.ExeConfigFilename = clientConfigFile;

                    var config = ConfigurationManager.OpenMappedExeConfiguration(customConfigFileMap, ConfigurationUserLevel.None);
                    var clientSettings = (AppSettingsSection)config.GetSection("appSettings");

                    var client = Load(Path.GetFileNameWithoutExtension(clientConfigFile), clientSettings);

                    var ldapClientIpSetting = clientSettings.Settings["ldap-client-ip"]?.Value;
                    if (string.IsNullOrEmpty(ldapClientIpSetting))
                    {
                        throw new Exception("Configuration error: 'ldap-client-ip' element not found");
                    }

                    var elements = ldapClientIpSetting.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var element in elements)
                    {
                        foreach (var ip in IPAddressRange.Parse(element))
                        {
                            configuration.AddClient(ip, client);
                        }
                    }
                }
            }


            return configuration;
        }

        private static ClientConfiguration Load(string name, AppSettingsSection appSettings)
        {
            var ldapServerSetting                               = appSettings.Settings["ldap-server"]?.Value;
            var multifactorApiKeySetting                        = appSettings.Settings["multifactor-nas-identifier"]?.Value;
            var multifactorApiSecretSetting                     = appSettings.Settings["multifactor-shared-secret"]?.Value;
            var serviceAccountsSetting                          = appSettings.Settings["ldap-service-accounts"]?.Value;
            var serviceAccountsOrganizationUnitSetting          = appSettings.Settings["ldap-service-accounts-ou"]?.Value;
            var activeDirectoryGroupSetting                     = appSettings.Settings["active-directory-group"]?.Value;
            var activeDirectory2FaGroupSetting                  = appSettings.Settings["active-directory-2fa-group"]?.Value;
            var bypassSecondFactorWhenApiUnreachableSetting     = appSettings.Settings["bypass-second-factor-when-api-unreachable"]?.Value;


            if (string.IsNullOrEmpty(ldapServerSetting))
            {
                throw new Exception("Configuration error: 'ldap-server' element not found");
            }
            if (string.IsNullOrEmpty(multifactorApiKeySetting))
            {
                throw new Exception("Configuration error: 'multifactor-nas-identifier' element not found");
            }
            if (string.IsNullOrEmpty(multifactorApiSecretSetting))
            {
                throw new Exception("Configuration error: 'multifactor-shared-secret' element not found");
            }

            var configuration = new ClientConfiguration
            {
                Name = name,
                LdapServer = ldapServerSetting,
                MultifactorApiKey = multifactorApiKeySetting,
                MultifactorApiSecret = multifactorApiSecretSetting,
                ActiveDirectoryGroup = activeDirectoryGroupSetting,
                ActiveDirectory2FaGroup = activeDirectory2FaGroupSetting
            };

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
