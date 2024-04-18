using MultiFactor.Ldap.Adapter.Services;

namespace MultiFactor.Ldap.Adapter.Core.NameResolving
{   
    public class NameResolverContext
    {
        public NetbiosDomainName[] Domains { get; }
        public LdapProfile Profile { get; }

        public NameResolverContext(NetbiosDomainName[] domains , LdapProfile profile)
        {
            Domains = domains ?? throw new System.ArgumentNullException(nameof(domains));
            Profile = profile;
        }
    }
}
