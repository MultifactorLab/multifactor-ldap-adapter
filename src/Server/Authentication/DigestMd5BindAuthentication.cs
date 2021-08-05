//Copyright(c) 2021 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-ldap-adapter/blob/main/LICENSE.md

using MultiFactor.Ldap.Adapter.Core;
using Serilog;
using System.Text.RegularExpressions;

namespace MultiFactor.Ldap.Adapter.Server.Authentication
{
    //https://datatracker.ietf.org/doc/html/rfc2831
    public class DigestMd5BindAuthentication : BindAuthentication
    {
        public DigestMd5BindAuthentication(ILogger logger) : base(logger)
        {
        }

        public override string MechanismName => "DIGEST-MD5";

        public override string Parse(LdapAttribute bindRequest)
        {
            var creds = bindRequest.ChildAttributes[2].ChildAttributes[1].GetValue<string>();
            if (!string.IsNullOrEmpty(creds))
            {
                var pattern = "username=\"(.*?)\"";
                var match = Regex.Match(creds, pattern);
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
            }

            return null;
        }
    }
}