namespace MultiFactor.Ldap.Adapter.Configuration.Core;

public interface IConfigurationProvider
{
    public Config GetRootConfiguration();
    public Config[] GetClientConfiguration();
}
