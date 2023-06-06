using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;

namespace MultiFactor.Ldap.Adapter.Configuration.Injectors
{
    public class ConfigurationProvider : IConfigurationProvider
    {
        public List<System.Configuration.Configuration> GetClientConfiguration()
        {
            var clientConfigFilesPath = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory) + Path.DirectorySeparatorChar + "clients";
            var clientConfigFiles = Directory.Exists(clientConfigFilesPath) ? Directory.GetFiles(clientConfigFilesPath, "*.config") : new string[0];
            return clientConfigFiles.Select(clientConfigFile =>
            {
                var customConfigFileMap = new ExeConfigurationFileMap();
                customConfigFileMap.ExeConfigFilename = clientConfigFile;

                return ConfigurationManager.OpenMappedExeConfiguration(customConfigFileMap, ConfigurationUserLevel.None);
            }).ToList();
        }

        public System.Configuration.Configuration GetRootConfiguration()
        {
            return ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
        }
    }
}
