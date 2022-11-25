using System.Net;

namespace MultiFactor.Ldap.Adapter.Configuration
{
    public interface ILdapServerConfig
    {
        /// <summary>
        /// This service LDAP endpoint
        /// </summary>
        IPEndPoint AdapterLdapEndpoint { get; }

        /// <summary>
        /// This service LDAPS endpoint
        /// </summary>
        IPEndPoint AdapterLdapsEndpoint { get; }
    }
}
