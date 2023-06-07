using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;

namespace MultiFactor.Ldap.Adapter.Configuration.Injectors
{
    public class ConfigurationProvider : IConfigurationProvider
    {
        public System.Configuration.Configuration[] GetClientConfiguration()
        {
            var clientConfigFilesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "clients");
            var clientConfigFiles = Directory.Exists(clientConfigFilesPath) ? Directory.GetFiles(clientConfigFilesPath, "*.config") : new string[0];
            return clientConfigFiles.Select(clientConfigFile =>
            {
                var customConfigFileMap = new ExeConfigurationFileMap();
                customConfigFileMap.ExeConfigFilename = clientConfigFile;

                return ConfigurationManager.OpenMappedExeConfiguration(customConfigFileMap, ConfigurationUserLevel.None);
            }).ToArray();
        }

        public System.Configuration.Configuration GetRootConfiguration()
        {
            return ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
        }
    }
}
