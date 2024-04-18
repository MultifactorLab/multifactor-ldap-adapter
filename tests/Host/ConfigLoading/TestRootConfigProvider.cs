using MultiFactor.Ldap.Adapter.Configuration.Core;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Config = System.Configuration.Configuration;

namespace MultiFactor.Ldap.Adapter.Tests.Fixtures.ConfigLoading
{
    internal class TestConfigProvider : IConfigurationProvider
    {
        private readonly TestConfigProviderOptions _options;

        public TestConfigProvider(TestConfigProviderOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public Config GetRootConfiguration()
        {
            if (string.IsNullOrWhiteSpace(_options.RootConfigFilePath))
            {
                return ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            }

            var customConfigFileMap = new ExeConfigurationFileMap
            {
                ExeConfigFilename = _options.RootConfigFilePath
            };
            return ConfigurationManager.OpenMappedExeConfiguration(customConfigFileMap, ConfigurationUserLevel.None, true);
        }

        public Config[] GetClientConfiguration()
        {
            return _options.ClientConfigFilePaths.Select(path => {
                var customConfigFileMap = new ExeConfigurationFileMap
                {
                    ExeConfigFilename = path
                };
                return ConfigurationManager.OpenMappedExeConfiguration(customConfigFileMap, ConfigurationUserLevel.None, true);
            }).ToArray();
        }
    }
}