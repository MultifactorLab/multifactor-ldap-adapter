using MultiFactor.Ldap.Adapter.Core;
using MultiFactor.Ldap.Adapter.Core.Abstractions;
using MultiFactor.Ldap.Adapter.Core.Requests;
using MultiFactor.Ldap.Adapter.Server.Authentication;
using Serilog;
using System;
using System.Linq;

namespace MultiFactor.Ldap.Adapter.Server.LdapPacketModifiers
{
    public class BindRequestModifier : IRequestModifier<BindRequest>
    {
        private readonly string _bindDn;
        private readonly ILogger _logger;

        public BindRequestModifier(string bindDn, ILogger logger)
        {
            _bindDn = bindDn;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public BindRequest Modify(BindRequest request)
        {
            if (request is null) throw new ArgumentNullException(nameof(request));

            var auth = new BindAuthenticationFactory(_logger).GetAuthentication(request.BindAttribute);
            if (auth == null || auth.MechanismName != "Simple") return request;
            if (string.IsNullOrWhiteSpace(_bindDn)) return request;

            if (!auth.TryParse(request.BindAttribute, out var username))
            {
                return request;
            }

            var id = LdapUserIdentity.Parse(username);

            var modifiedPacket = new LdapPacket(request.Packet.MessageId);
            LdapAttribute bindAttr = null;
            foreach (var attr in request.Packet.ChildAttributes.Skip(1))
            {
                if (attr.LdapOperation != LdapOperation.BindRequest)
                {
                    modifiedPacket.ChildAttributes.Add(attr);
                    continue;
                }

                bindAttr = new LdapAttribute(LdapOperation.BindRequest);

                for (var i = 0; i < attr.ChildAttributes.Count; i++)
                {
                    if (i != 1)
                    {
                        bindAttr.ChildAttributes.Add(attr.ChildAttributes[i]);
                        continue;
                    }

                    var enrichedUsername = EnrichUsername(id.GetUid());
                    bindAttr.ChildAttributes.Add(new LdapAttribute(UniversalDataType.OctetString, enrichedUsername));
                }

                modifiedPacket.ChildAttributes.Add(bindAttr);
            }

            return new BindRequest(modifiedPacket, bindAttr);
        }

        private string EnrichUsername(string username)
        {
            var bindDn = $"uid={username}";
            if (!string.IsNullOrEmpty(_bindDn))
            {
                return $"{bindDn},{_bindDn}";
            }

            return bindDn;
        }
    }
}
