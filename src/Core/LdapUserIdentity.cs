using System;

namespace MultiFactor.Ldap.Adapter.Core
{
    /// <summary>
    /// User identity.
    /// </summary>
    public class LdapUserIdentity
    {
        public string Name { get; }
        public IdentityType Type { get; }

        protected LdapUserIdentity(string name, IdentityType type)
        {
            Name = name;
            Type = type;
        }

        public static LdapUserIdentity Parse(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException($"'{nameof(username)}' cannot be null or whitespace.", nameof(username));
            }

            return new LdapUserIdentity(username, GetIdentityType(username));
        }

        public string GetUid()
        {
            return Type switch
            {
                IdentityType.sAMAccountName => Name,
                IdentityType.DistinguishedName => DnToCn(Name),
                IdentityType.UserPrincipalName => UpnToUid(Name),
                _ => throw new NotImplementedException($"Unexpected identity type: {Type}"),
            };
        }

        private static IdentityType GetIdentityType(string userName)
        {
            if (userName.Contains("@")) return IdentityType.UserPrincipalName;
            if (userName.Contains("CN=", StringComparison.OrdinalIgnoreCase)) return IdentityType.DistinguishedName;
            return IdentityType.sAMAccountName;
        }

        private static string DnToCn(string dn)
        {
            return dn.Split(',')[0].Split(new[] { '=' })[1];
        }

        private static string UpnToUid(string upn)
        {
            var index = upn.IndexOf('@');
            if (index == -1) throw new InvalidOperationException("Identity should be of UPN type");
            return upn.Substring(0, index);
        }
    }
}
