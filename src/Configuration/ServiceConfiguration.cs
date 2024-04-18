//Copyright(c) 2021 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-ldap-adapter/blob/main/LICENSE.md

using MultiFactor.Ldap.Adapter.Configuration.Core;
using MultiFactor.Ldap.Adapter.Core;
using NetTools;
using Serilog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using LdapIdentityFormat = MultiFactor.Ldap.Adapter.Core.NameResolve.LdapIdentityFormat;

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
        private readonly IDictionary<IPAddress, ClientConfiguration> _ipClients;
        private readonly ILogger _logger;
        private readonly IConfigurationProvider _configurationProvider;
        public ServiceConfiguration(IConfigurationProvider configurationProvider, ILogger logger)
        {
            _ipClients = new Dictionary<IPAddress, ClientConfiguration>();
            _logger = logger;
            _configurationProvider = configurationProvider;
            Load();
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

        public ILdapServerConfig ServerConfig { get; private set; }

        /// <summary>
        /// Multifactor API URL
        /// </summary>
        public string ApiUrl { get; set; }

        /// <summary>
        /// HTTP Proxy for API
        /// </summary>
        public string ApiProxy { get; set; }

        /// <summary>
        /// HTTP Timeout for Multifactor requests
        /// </summary>
        public TimeSpan ApiTimeout { get; set; }

        /// <summary>
        /// Logging level
        /// </summary>
        public string LogLevel { get; set; }

        /// <summary>
        /// Logging Format
        /// </summary>
        public string LogFormat { get; set; }

        /// <summary>
        /// Certificate for TLS
        /// </summary>
        public X509Certificate2 X509Certificate { get; set; }
        
        /// <summary>
        /// Certificate Password
        /// </summary>
        public string CertificatePassword { get; set; }

        public bool SingleClientMode { get; set; }

        public RandomWaiterConfig InvalidCredentialDelay { get; private set; }

        /// <summary>
        /// Read and load settings from appSettings configuration section
        /// </summary>
        private void Load()
        {
            var serviceConfig = _configurationProvider.GetRootConfiguration();

            var appSettingsSection = serviceConfig.GetSection("appSettings");
            var appSettings = appSettingsSection as AppSettingsSection;

            var apiUrlSetting                   = appSettings.Settings["multifactor-api-url"]?.Value;
            var apiProxySetting                 = appSettings.Settings["multifactor-api-proxy"]?.Value;
            var apiTimeoutSetting               = appSettings.Settings["multifactor-api-timeout"]?.Value;
            var logLevelSetting                 = appSettings.Settings["logging-level"]?.Value;
            var certificatePassword = appSettings.Settings["certificate-password"]?.Value;


            if (string.IsNullOrEmpty(apiUrlSetting))
            {
                throw new Exception("Configuration error: 'multifactor-api-url' element not found");
            }
            TimeSpan apiTimeout = ParseHttpTimeout(apiTimeoutSetting);
            if (string.IsNullOrEmpty(logLevelSetting))
            {
                throw new Exception("Configuration error: 'logging-level' element not found");
            }

            ApiUrl = apiUrlSetting;
            ApiTimeout = apiTimeout;
            ApiProxy = apiProxySetting;
            LogLevel = logLevelSetting;

            var ldapServerConfig = LdapServerConfig.Parse(appSettings);
            if (ldapServerConfig.IsEmpty)
            {
                throw new Exception("Configuration error: Neither 'adapter-ldap-endpoint' or 'adapter-ldaps-endpoint' configured");
            }
            
            ServerConfig = ldapServerConfig;

            if (!string.IsNullOrEmpty(certificatePassword))
            {
                CertificatePassword = certificatePassword;
            }

            try
            {
                InvalidCredentialDelay = RandomWaiterConfig.Create(appSettings.Settings[Constants.Configuration.PciDss.InvalidCredentialDelay]?.Value);
            }
            catch
            {
                throw new Exception($"Configuration error: Can't parse '{Constants.Configuration.PciDss.InvalidCredentialDelay}' value");
            }

            var clientConfigFiles = _configurationProvider.GetClientConfiguration();
            if (clientConfigFiles.Length == 0)
            {
                //check if we have anything
                var ldapServer = appSettings.Settings["ldap-server"]?.Value;
                if (ldapServer == null)
                {
                    throw new ConfigurationErrorsException("No clients' config files found. Use one of the *.template files in the /clients folder to customize settings. Then save this file as *.config.");
                }

                var userNameTransformRulesSection = serviceConfig.GetSection("UserNameTransformRules") as UserNameTransformRulesSection;

                var client = Load("General", appSettings, userNameTransformRulesSection);
                AddClient(IPAddress.Any, client);
                SingleClientMode = true;
            }
            else
            {
                foreach (var config in clientConfigFiles)
                {
                    _logger.Information($"Loading client configuration from {Path.GetFileName(config.FilePath)}");

                    var userNameTransformRulesSection = config.GetSection("UserNameTransformRules") as UserNameTransformRulesSection;
                    var clientSettings = (AppSettingsSection)config.GetSection("appSettings");
                    var client = Load(Path.GetFileNameWithoutExtension(config.FilePath), clientSettings, userNameTransformRulesSection);

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
                            AddClient(ip, client);
                        }
                    }
                }
            }
        }

        private static ClientConfiguration Load(string name, AppSettingsSection appSettings, UserNameTransformRulesSection userNameTransformRulesSection)
        {
            var ldapServerSetting                               = appSettings.Settings["ldap-server"]?.Value;
            var ldapBaseDnSetting                               = appSettings.Settings["ldap-base-dn"]?.Value;
            var multifactorApiKeySetting                        = appSettings.Settings["multifactor-nas-identifier"]?.Value;
            var multifactorApiSecretSetting                     = appSettings.Settings["multifactor-shared-secret"]?.Value;
            var serviceAccountsSetting                          = appSettings.Settings["ldap-service-accounts"]?.Value;
            var serviceAccountsOrganizationUnitSetting          = appSettings.Settings["ldap-service-accounts-ou"]?.Value;
            var activeDirectoryGroupSetting                     = appSettings.Settings["active-directory-group"]?.Value;
            var activeDirectory2FaGroupSetting                  = appSettings.Settings["active-directory-2fa-group"]?.Value;
            var activeDirectory2FaBypassGroupSetting            = appSettings.Settings["active-directory-2fa-bypass-group"]?.Value;
            var bypassSecondFactorWhenApiUnreachableSetting     = appSettings.Settings["bypass-second-factor-when-api-unreachable"]?.Value;
            var loadActiveDirectoryNestedGroupsSettings         = appSettings.Settings["load-active-directory-nested-groups"]?.Value;
            var logLevel                                        = appSettings.Settings["logging-level"]?.Value;
            var logFormat                                       = appSettings.Settings["logging-format"]?.Value;
            var transformLdapIdentityString                     = appSettings.Settings["transform-ldap-identity"]?.Value;

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

            LdapIdentityFormat transformLdapIdentityFormat = LdapIdentityFormat.None;
            if (!string.IsNullOrEmpty(transformLdapIdentityString) && 
                !Enum.TryParse<LdapIdentityFormat>(transformLdapIdentityString, true, out transformLdapIdentityFormat))
            {
                throw new Exception("Configuration error: 'transform-ldap-identity' element has a wrong value");
            }
            
            var configuration = new ClientConfiguration
            {
                Name = name,
                LdapServer = ldapServerSetting,
                MultifactorApiKey = multifactorApiKeySetting,
                MultifactorApiSecret = multifactorApiSecretSetting,
                LdapBaseDn = ldapBaseDnSetting,
                LdapIdentityFormat = transformLdapIdentityFormat
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

            if (!string.IsNullOrEmpty(activeDirectoryGroupSetting))
            {
                configuration.ActiveDirectoryGroup = activeDirectoryGroupSetting.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            }

            if (!string.IsNullOrEmpty(activeDirectory2FaGroupSetting))
            {
                configuration.ActiveDirectory2FaGroup = activeDirectory2FaGroupSetting.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            }

            if (!string.IsNullOrEmpty(activeDirectory2FaBypassGroupSetting))
            {
                configuration.ActiveDirectory2FaBypassGroup = activeDirectory2FaBypassGroupSetting.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            }

            if (!string.IsNullOrEmpty(bypassSecondFactorWhenApiUnreachableSetting))
            {
                if (!bool.TryParse(bypassSecondFactorWhenApiUnreachableSetting, out var bypassSecondFactorWhenApiUnreachable))
                {
                    throw new Exception("Configuration error: Can't parse 'bypass-second-factor-when-api-unreachable' value");
                }

                configuration.BypassSecondFactorWhenApiUnreachable = bypassSecondFactorWhenApiUnreachable;
            }

            if (!string.IsNullOrEmpty(loadActiveDirectoryNestedGroupsSettings))
            {
                if (!bool.TryParse(loadActiveDirectoryNestedGroupsSettings, out var loadActiveDirectoryNestedGroups))
                {
                    throw new Exception("Configuration error: Can't parse 'load-active-directory-nested-groups' value");
                }

                configuration.LoadActiveDirectoryNestedGroups = loadActiveDirectoryNestedGroups;
            }

            if(!string.IsNullOrEmpty(logLevel))
            {
                configuration.LogLevel = logLevel;
            }

            if(!string.IsNullOrEmpty(logFormat))
            {
                configuration.LogFormat = logFormat;
            }

            if(userNameTransformRulesSection != null)
            {
                configuration.UserNameTransformRules.Load(userNameTransformRulesSection);
            }

            try
            {
                configuration.AuthenticationCacheLifetime = AuthenticatedClientCacheConfig
                    .Create(appSettings.Settings[Constants.Configuration.AuthenticationCacheLifetime]?.Value);
            }
            catch
            {
                throw new Exception($"Configuration error: Can't parse '{Constants.Configuration.AuthenticationCacheLifetime}' value");
            }

            return configuration;
        }

        public static string GetLogFormat()
        {
            var appSettings = ConfigurationManager.AppSettings;
            return appSettings?["logging-format"];
        }

        public static string GetLogLevel()
        {
            var appSettings = ConfigurationManager.AppSettings;
            return appSettings?["logging-level"];
        }

        private static TimeSpan ParseHttpTimeout(string mfTimeoutSetting)
        {
            TimeSpan _minimalApiTimeout = TimeSpan.FromSeconds(65);

            if (!TimeSpan.TryParseExact(mfTimeoutSetting, @"hh\:mm\:ss", null, System.Globalization.TimeSpanStyles.None, out var httpRequestTimeout))
                return _minimalApiTimeout;

            return httpRequestTimeout == TimeSpan.Zero ?
                Timeout.InfiniteTimeSpan // infinity timeout
                : httpRequestTimeout < _minimalApiTimeout
                    ? _minimalApiTimeout  // minimal timeout
                    : httpRequestTimeout; // timeout from config
        }
    }
}
