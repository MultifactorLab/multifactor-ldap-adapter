using MultiFactor.Ldap.Adapter.Core.Abstractions;
using MultiFactor.Ldap.Adapter.Core.Requests;

namespace MultiFactor.Ldap.Adapter.Server.LdapPacketModifiers
{
    public class GenericBindRequestModifier : IRequestModifier<GenericRequest>
    {
        public GenericRequest Modify(GenericRequest request) => request;
    }
}
