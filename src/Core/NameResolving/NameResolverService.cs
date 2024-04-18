using MultiFactor.Ldap.Adapter.Core.NameResolving;
using MultiFactor.Ldap.Adapter.Core.NameResolving.NameTranslators;
using Serilog;

namespace MultiFactor.Ldap.Adapter.Core.NameResolve
{
    public class NameResolverService
    {
        private ILogger _logger;

        public NameResolverService(ILogger logger)
        {
            _logger = logger;
        }

        public string Resolve(NameResolverContext context, string name, LdapIdentityFormat to)
        {
            var from = NameTypeDetector.GetType(name);
            if (from == null)
            {
                return name;
            }

            var resolver = GetTranslator(context, (LdapIdentityFormat)from, to);
            if (resolver == null)
            {
                return name;
            }
            return resolver.Translate(context, name);
        }

        private INameTranslator GetTranslator(NameResolverContext context, LdapIdentityFormat from, LdapIdentityFormat to)
        {
            if (from == LdapIdentityFormat.UidAndNetbios && to  == LdapIdentityFormat.Upn)
            {
                return new sAMAccountNameAndNetbiosToUpnNameTranslator();
            }
            else if (from == LdapIdentityFormat.NetBIOSAndUid && to == LdapIdentityFormat.Upn)
            {
                return new NetbiosToUpnNameTranslator();
            }
            else if (from == LdapIdentityFormat.DistinguishedName && to == LdapIdentityFormat.Upn)
            {
                return new DistinguishedNameToUpnTranslator();
            }
            // There are a case when sAMAccountName@domain.local looks exactly like UPN
            // Let's try an UPN we got from the profile
            if (from == LdapIdentityFormat.Upn && to == LdapIdentityFormat.Upn && context.Profile != null)
            {
                return new UpnFromProfileNameTranslator();
            }
            if(from == LdapIdentityFormat.SamAccountName && to == LdapIdentityFormat.Upn)
            {
                return new sAMAccountNameToUpnNameTranslator();
            }
            _logger.Error($"Suitable username format was not found");
            return null;
        }
    }
}
