using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace MultiFactor.Ldap.Adapter.Core.NameResolve
{
    public class NameTypeDetector
    {
        public static NameType? GetType(string name)
        {
            if (name.Contains('\\'))
            {
                return NameType.NetBIOSAndUid;
            }
            if (name.Contains("CN=", StringComparison.OrdinalIgnoreCase))
            {
                return NameType.DistinguishedName;
            }
            var domainRegex = new Regex("^[^@]+@(.+)$");
            var domainMatch = domainRegex.Match(name);
            if (!domainMatch.Success || domainMatch.Groups.Count < 2)
            {
                return NameType.SamAccountName;
            }
            return domainMatch.Groups[1].Value.Count(x => x == '.') == 0 
                ? NameType.UidAndNetbios
                : NameType.Upn;
        } 
    }
}
