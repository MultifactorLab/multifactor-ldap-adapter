using MultiFactor.Ldap.Adapter.Core;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiFactor.Ldap.Adapter.Server.Authentication
{
    public class SpnegoNtlmBindAuthentication : NtlmBindAuthentication
    {
        public override string MechanismName => "SPNEGO-NTLM";

        protected override byte[] GetAuthPacket(LdapAttribute bindRequest)
        {
            return bindRequest.ChildAttributes[2].ChildAttributes[1].Value;
        }

        public SpnegoNtlmBindAuthentication(ILogger logger) : base(logger)
        {
        }
    }
}
