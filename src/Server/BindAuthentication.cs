//Copyright(c) 2021 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-ldap-adapter/blob/main/LICENSE.md

using MultiFactor.Ldap.Adapter.Core;
using Serilog;
using System;
using System.Linq;
using System.Text;

namespace MultiFactor.Ldap.Adapter.Server
{
    public abstract class BindAuthentication
    {
        protected ILogger _logger;

        protected BindAuthentication(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public abstract string MechanismName { get;  }
        public abstract string Parse(LdapAttribute bindRequest);

        public bool TryParse(LdapAttribute bindRequest, out string userName)
        {
            try
            {
                userName = Parse(bindRequest);
                return true;
            }
            catch(Exception ex)
            {
                _logger.Warning(ex, $"Can't parse bind {MechanismName} request");
                userName = null;
                return false;
            }
        }

        public static bool IsNtlm(byte[] data)
        {
            var first8bytes = data.Take(8).ToArray();
            var signature = Encoding.ASCII.GetString(first8bytes);

            return signature == "NTLMSSP\0";
        }
    }
}