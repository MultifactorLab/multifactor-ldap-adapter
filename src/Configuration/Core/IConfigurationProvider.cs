
namespace MultiFactor.Ldap.Adapter.Configuration.Core
{
    public interface IConfigurationProvider
    {
        public System.Configuration.Configuration GetRootConfiguration();

        public System.Configuration.Configuration[] GetClientConfiguration();

    }
}
