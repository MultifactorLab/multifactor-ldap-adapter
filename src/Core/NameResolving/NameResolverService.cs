using MultiFactor.Ldap.Adapter.Core.NameResolving;
using MultiFactor.Ldap.Adapter.Core.NameResolving.NameTranslators;

namespace MultiFactor.Ldap.Adapter.Core.NameResolve
{
    public class NameResolverService
    {
        public string Resolve(NameResolverContext context, string name, LdapIdentityFormat to)
        {
            var from = NameTypeDetector.GetType(name);
            if(from == null)
            {
                return name;
            }

            var resolver = GetTranslator(context, (LdapIdentityFormat)from, to);
            if(resolver == null)
            {
                return name;
            }
            return resolver.Translate(context, name);
        }


        public INameTranslator GetTranslator(NameResolverContext context, LdapIdentityFormat from, LdapIdentityFormat to)
        {
            // TODO AddTranslators
            if(from == LdapIdentityFormat.UidAndNetbios && to  == LdapIdentityFormat.Upn)
            {
                return new sAMAccountNameAndNetbiosToUpnNameTranslator();
            }
            else if(from == LdapIdentityFormat.NetBIOSAndUid && to == LdapIdentityFormat.Upn)
            {
                return new NetbiosToUpnNameTranslator();
            }
            else if(from == LdapIdentityFormat.DistinguishedName && to == LdapIdentityFormat.Upn)
            {
                return new DistinguishedNameToUpnTranslator();
            }
            // There are a case when sAMAccountName@domain.local looks exactly like UPN
            // Let's try an UPN we got from the profile
            if(from == LdapIdentityFormat.Upn && to == LdapIdentityFormat.Upn && context.Profile != null)
            {
                return new UpnFromProfileNameTranslator();
            }
            return null;
        }
    }
}
