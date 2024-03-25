using MultiFactor.Ldap.Adapter.Core.NameResolving;
using MultiFactor.Ldap.Adapter.Core.NameResolving.NameTranslators;

namespace MultiFactor.Ldap.Adapter.Core.NameResolve
{
    public class NameResolverService
    {
        public string Resolve(NameResolverContext context, string name, NameType to)
        {
            var from = NameTypeDetector.GetType(name);
            if(from == null)
            {
                return name;
            }

            var resolver = GetTranslator(context, (NameType)from, to);
            if(resolver == null)
            {
                return name;
            }
            return resolver.Translate(context, name);
        }


        public INameTranslator GetTranslator(NameResolverContext context, NameType from, NameType to)
        {
            // TODO AddTranslators
            if(from == NameType.UidAndNetbios && to  == NameType.Upn)
            {
                return new sAMAccountNameAndNetbiosToUpnNameTranslator();
            }
            else if(from == NameType.NetBIOSAndUid && to == NameType.Upn)
            {
                return new NetbiosToUpnNameTranslator();
            }
            else if(from == NameType.DistinguishedName && to == NameType.Upn)
            {
                return new DistinguishedNameToUpnTranslator();
            }
            // There are a case when sAMAccountName@domain.local looks exactly like UPN
            // Let's try an UPN we got from the profile
            if(from == NameType.Upn && to == NameType.Upn && context.Profile != null)
            {
                return new UpnFromProfileNameTranslator();
            }
            return null;
        }
    }
}
