using MultiFactor.Ldap.Adapter.Services;

namespace MultiFactor.Ldap.Adapter.Core.NameResolving
{   
    public class NameResolverContext
    {
        public NetbiosDomainName[] Domains { get; private set; }
        public LdapProfile Profile { get; private set; }

        public NameResolverContext(NetbiosDomainName[] domains, LdapProfile profile)
        {
            Domains = domains;
            Profile = profile;
        }   
    }
}
