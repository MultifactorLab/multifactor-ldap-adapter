//Copyright(c) 2021 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-ldap-adapter/blob/main/LICENSE.md

using MultiFactor.Ldap.Adapter.Configuration;
using MultiFactor.Ldap.Adapter.Core;
using MultiFactor.Ldap.Adapter.Services;
using Serilog;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Threading.Tasks;

namespace MultiFactor.Ldap.Adapter.Server
{
    internal class SecureLdapServer : LdapServerBase, ILdapServer
    {
        public SecureLdapServer(ServiceConfiguration configuration, LdapProxyFactory proxyFactory, ILogger logger) 
            : base(configuration, proxyFactory, logger) { }

        public bool Enabled => _serviceConfiguration.ServerConfig.AdapterLdapsEndpoint != null;

        protected override async Task<Stream> GetClientStream(TcpClient client)
        {
            var stream = await base.GetClientStream(client);
            var tlsStream = new SslStream(stream, false);

            var tlsProcotols = SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12;
            await tlsStream.AuthenticateAsServerAsync(_serviceConfiguration.X509Certificate, clientCertificateRequired: false, tlsProcotols, checkCertificateRevocation: false);

            return tlsStream;
        }

        public override void Start()
        {
            TlsCertificateFactory.EnsureTlsCertificatesExist(Constants.ApplicationPath, _serviceConfiguration, _logger);
            base.Start();
        }

        protected override IPEndPoint GetLocalEndpoint() => _serviceConfiguration.ServerConfig.AdapterLdapsEndpoint;

        protected override void LogStart()
        {
            _logger.Information($"Starting ldaps server on {GetLocalEndpoint()}");
        }

        protected override void LogStop()
        {
            _logger.Information($"Stopping ldaps server on {GetLocalEndpoint}");
        }
    }
}
