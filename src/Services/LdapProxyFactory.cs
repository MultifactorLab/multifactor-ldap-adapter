using MultiFactor.Ldap.Adapter.Configuration;
using MultiFactor.Ldap.Adapter.Core.Logging;
using MultiFactor.Ldap.Adapter.Core.NameResolve;
using MultiFactor.Ldap.Adapter.Server;
using System.IO;
using System.Net.Sockets;

namespace MultiFactor.Ldap.Adapter.Services
{
    public class LdapProxyFactory
    {
        private readonly MultiFactorApiClient _apiClient;
        private readonly ClientLoggerProvider _loggerProvider;
        private readonly RandomWaiter _randomWaiter;
        private readonly NameResolverService _nameResolverService;

        public LdapProxyFactory(MultiFactorApiClient apiClient, ClientLoggerProvider loggerProvider,
            RandomWaiter randomWaiter,
            NameResolverService nameResolverService)
        {
            _apiClient = apiClient;
            _loggerProvider = loggerProvider;
            _randomWaiter = randomWaiter;
            _nameResolverService = nameResolverService;
        }

        public LdapProxy CreateProxy(TcpClient clientConn, Stream clientStream, TcpClient serverConn, Stream serverStream, ClientConfiguration clientConfig)
        {
            return new LdapProxy(
                clientConn,
                clientStream,
                serverConn,
                serverStream,
                clientConfig,
                _apiClient,
                _randomWaiter,
                _loggerProvider.GetLogger(clientConfig),
                _nameResolverService);
        }
    }
}
