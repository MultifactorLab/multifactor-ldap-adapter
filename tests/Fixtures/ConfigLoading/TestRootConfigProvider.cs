using MultiFactor.Ldap.Adapter.Configuration.Injectors;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace MultiFactor.Ldap.Adapter.Tests.Fixtures.ConfigLoading
{
    internal class TestConfigProvider : IConfigurationProvider
    {
        private readonly TestConfigProviderOptions _options;

        public TestConfigProvider(TestConfigProviderOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public System.Configuration.Configuration GetRootConfiguration()
        {
            if (string.IsNullOrWhiteSpace(_options.RootConfigFilePath))
            {
                return ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            }

            var customConfigFileMap = new ExeConfigurationFileMap
            {
                ExeConfigFilename = _options.RootConfigFilePath
            };
            return ConfigurationManager.OpenMappedExeConfiguration(customConfigFileMap, ConfigurationUserLevel.None);
        }

        public List<System.Configuration.Configuration> GetClientConfiguration()
        {
            return _options.ClientConfigFilePaths.Select(path => {
                var customConfigFileMap = new ExeConfigurationFileMap
                {
                    ExeConfigFilename = path
                };
                return ConfigurationManager.OpenMappedExeConfiguration(customConfigFileMap, ConfigurationUserLevel.None);
            }).ToList();
        }
    }
}