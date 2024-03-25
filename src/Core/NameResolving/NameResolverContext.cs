using MultiFactor.Ldap.Adapter.Services;

namespace MultiFactor.Ldap.Adapter.Core.NameResolving
{   
    public class NameResolverContext
    {
        private NetbiosDomainName[] _domains;
        public NetbiosDomainName[] Domains => _domains;


        private string _baseDn;
        public string BaseDn => _baseDn;

        
        private LdapProfile _profile;
        public LdapProfile Profile => _profile;

        public NameResolverContext SetDomains(NetbiosDomainName[] domains)
        {
            this._domains = domains;
            return this;
        }

        public NameResolverContext SetBaseDn(string baseDn)
        {
            this._baseDn = baseDn;
            return this;
        }


        public NameResolverContext SetMatchedProfile(LdapProfile profile)
        {
            this._profile = profile;
            return this;
        }
    }
}
