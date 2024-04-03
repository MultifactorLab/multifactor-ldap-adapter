using MultiFactor.Ldap.Adapter.Services;

namespace MultiFactor.Ldap.Adapter.Core.NameResolving
{   
    public class NameResolverContext
    {
        public NetbiosDomainName[] Domains;
        public LdapProfile Profile;
    }
}
