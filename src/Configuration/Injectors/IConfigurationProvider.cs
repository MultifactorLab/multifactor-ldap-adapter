
namespace MultiFactor.Ldap.Adapter.Configuration.Injectors
{
    public interface IConfigurationProvider
    {
        public System.Configuration.Configuration GetRootConfiguration();

        public System.Configuration.Configuration[] GetClientConfiguration();

    }
}
