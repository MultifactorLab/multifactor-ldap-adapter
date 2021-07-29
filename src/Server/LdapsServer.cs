//Copyright(c) 2021 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-ldap-adapter/blob/main/LICENSE.md

using Serilog;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Threading.Tasks;

namespace MultiFactor.Ldap.Adapter.Server
{
    public class LdapsServer : LdapServer
    {
        public LdapsServer(IPEndPoint localEndpoint, Configuration configuration, ILogger logger) : base(localEndpoint, configuration, logger)
        {
        }

        protected override async Task<Stream> GetClientStream(TcpClient client)
        {
            var stream = await base.GetClientStream(client);
            var tlsStream = new SslStream(stream, false);

            var tlsProcotols = SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12;
            await tlsStream.AuthenticateAsServerAsync(_configuration.X509Certificate, clientCertificateRequired: false, tlsProcotols, checkCertificateRevocation: false);

            return tlsStream;
        }

        protected override void LogStart()
        {
            _logger.Information($"Starting ldaps server on {_localEndpoint}");
        }

        protected override void LogStop()
        {
            _logger.Information($"Stopping ldaps server on {_localEndpoint}");
        }
    }
}
