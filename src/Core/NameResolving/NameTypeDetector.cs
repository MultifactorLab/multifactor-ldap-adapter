using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace MultiFactor.Ldap.Adapter.Core.NameResolve
{
    public class NameTypeDetector
    {
        private static readonly Regex _domainRegex = new("^[^@]+@(.+)$");

        public static LdapIdentityFormat? GetType(string name)
        {
            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (name.Contains('\\'))
            {
                return LdapIdentityFormat.NetBIOSAndUid;
            }

            if (name.Contains("CN=", StringComparison.OrdinalIgnoreCase))
            {
                return LdapIdentityFormat.DistinguishedName;
            }

            var domainMatch = _domainRegex.Match(name);
            if (!domainMatch.Success || domainMatch.Groups.Count < 2)
            {
                return LdapIdentityFormat.SamAccountName;
            }

            return domainMatch.Groups[1].Value.Count(x => x == '.') == 0 
                ? LdapIdentityFormat.UidAndNetbios
                : LdapIdentityFormat.Upn;
        } 
    }
}
