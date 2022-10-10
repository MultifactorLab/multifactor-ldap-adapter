using MultiFactor.Ldap.Adapter.Core;
using Serilog;
using System;

namespace MultiFactor.Ldap.Adapter.Server.Authentication
{
    public class BindAuthenticationFactory
    {
        private readonly ILogger _logger;

        public BindAuthenticationFactory(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Analyzes cpecified bind request attribute and returns the corresponding authentication object.
        /// </summary>
        /// <param name="bindRequest">Request.</param>
        /// <returns>Authentication object.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public BindAuthentication GetAuthentication(LdapAttribute bindRequest)
        {
            if (bindRequest is null) throw new ArgumentNullException(nameof(bindRequest));
            if (bindRequest.LdapOperation != LdapOperation.BindRequest)
            {
                throw new InvalidOperationException("This is not a BindRequest");
            }
            
            var authPacket = bindRequest.ChildAttributes[2];
            if (!authPacket.IsConstructed)  //simple bind or NTLM
            {
                if (BindAuthentication.IsNtlm(authPacket.Value))
                {
                    return new NtlmBindAuthentication(_logger);
                }

                return new SimpleBindAuthentication(_logger);
            }

            var mechanism = authPacket.ChildAttributes[0].GetValue<string>();

            if (mechanism == "DIGEST-MD5")
            {
                return new DigestMd5BindAuthentication(_logger);
            }

            if (mechanism == "GSS-SPNEGO")
            {
                var saslPacket = authPacket.ChildAttributes[1].Value;
                if (BindAuthentication.IsNtlm(saslPacket))
                {
                    return new SpnegoNtlmBindAuthentication(_logger);
                }
            }

            //kerberos or not-implemented
            //_logger.Debug($"Unknown bind mechanism: {mechanism}");

            return null;
        }
    }
}
