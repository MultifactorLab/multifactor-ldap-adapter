//Copyright(c) 2021 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-ldap-adapter/blob/main/LICENSE.md

using MultiFactor.Ldap.Adapter.Core;
using Serilog;

namespace MultiFactor.Ldap.Adapter.Server.Authentication
{
    public class SimpleBindAuthentication : BindAuthentication
    {
        public SimpleBindAuthentication(ILogger logger) : base(logger)
        {
        }

        public override string MechanismName => "Simple";

        public override string Parse(LdapAttribute bindRequest)
        {
            var userName = bindRequest.ChildAttributes[1].GetValue<string>();
        
            if (userName == "NTLM")
            {
                return null; //MS NTLM
            }

            return userName;
        }
    }
}