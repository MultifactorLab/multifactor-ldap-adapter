using MultiFactor.Ldap.Adapter.Core.Abstractions;
using MultiFactor.Ldap.Adapter.Core.Requests;

namespace MultiFactor.Ldap.Adapter.Server.LdapPacketModifiers
{
    public class SearchRequestModifier : IRequestModifier<SearchRequest>
    {
        public SearchRequest Modify(SearchRequest request) => request;
    }
}
