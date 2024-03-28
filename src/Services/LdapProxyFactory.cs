using MultiFactor.Ldap.Adapter.Configuration;
using MultiFactor.Ldap.Adapter.Core.Logging;
using MultiFactor.Ldap.Adapter.Server;
using MultiFactor.Ldap.Adapter.Services.SecondFactor;
using System.IO;
using System.Net.Sockets;

namespace MultiFactor.Ldap.Adapter.Services
{
    public class LdapProxyFactory
    {
        private readonly SecondFactorVerifier _secondFactorVerifier;
        private readonly ClientLoggerProvider _loggerProvider;
        private readonly RandomWaiter _randomWaiter;

        public LdapProxyFactory(
            SecondFactorVerifier secondFactorVerifier,
            ClientLoggerProvider loggerProvider,
            RandomWaiter randomWaiter)
        {
            _secondFactorVerifier = secondFactorVerifier;
            _loggerProvider = loggerProvider;
            _randomWaiter = randomWaiter ?? throw new System.ArgumentNullException(nameof(randomWaiter));
        }

        public LdapProxy CreateProxy(TcpClient clientConn, Stream clientStream, TcpClient serverConn, Stream serverStream, ClientConfiguration clientConfig)
        {
            return new LdapProxy(
                clientConn,
                clientStream,
                serverConn,
                serverStream,
                clientConfig,
                _secondFactorVerifier,
                _randomWaiter,
                _loggerProvider.GetLogger(clientConfig));
        }
    }
}
