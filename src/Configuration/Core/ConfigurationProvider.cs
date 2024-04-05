using System;
using System.Configuration;
using System.IO;
using System.Linq;

namespace MultiFactor.Ldap.Adapter.Configuration.Core
{
    public class ConfigurationProvider : IConfigurationProvider
    {
        public Config[] GetClientConfiguration()
        {
            var clientConfigFilesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "clients");
            var clientConfigFiles = Directory.Exists(clientConfigFilesPath) 
                ? Directory.GetFiles(clientConfigFilesPath, "*.config", SearchOption.AllDirectories) 
                : Array.Empty<string>();
            return clientConfigFiles.Select(clientConfigFile =>
            {
                var customConfigFileMap = new ExeConfigurationFileMap
                {
                    ExeConfigFilename = clientConfigFile
                };

                return ConfigurationManager.OpenMappedExeConfiguration(customConfigFileMap, ConfigurationUserLevel.None);
            }).ToArray();
        }

        public Config GetRootConfiguration()
        {
            return ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
        }
    }
}
