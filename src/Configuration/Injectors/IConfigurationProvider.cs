using System.Collections.Generic;

namespace MultiFactor.Ldap.Adapter.Configuration.Injectors
{
    public interface IConfigurationProvider
    {
        public System.Configuration.Configuration GetRootConfiguration();

        public List<System.Configuration.Configuration> GetClientConfiguration();

    }
}
