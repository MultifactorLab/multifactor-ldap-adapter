using MultiFactor.Ldap.Adapter.Configuration;
using MultiFactor.Ldap.Adapter.Services;
using Serilog;
//Copyright(c) 2021 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-ldap-adapter/blob/main/LICENSE.md

using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace MultiFactor.Ldap.Adapter.Server
{
    internal abstract class LdapServerBase
    {
        private bool _started = false;
        private readonly LdapProxyFactory _proxyFactory;

        protected readonly ServiceConfiguration _serviceConfiguration;
        protected readonly ILogger _logger;

        protected TcpListener _server;

        public LdapServerBase(ServiceConfiguration serviceConfiguration,
            LdapProxyFactory proxyFactory,
            ILogger logger)
        {
            _serviceConfiguration = serviceConfiguration ?? throw new ArgumentNullException(nameof(serviceConfiguration));
            _proxyFactory = proxyFactory ?? throw new ArgumentNullException(nameof(proxyFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Start listening for requests
        /// </summary>
        public virtual void Start()
        {
            if (_started)
            {
                return;
            }
            _started = true;

            LogStart();
            _server = new TcpListener(GetLocalEndpoint());
            _server.Start();
            _ = Receive();
            _logger.Information("Server started on {endpoint}", GetLocalEndpoint());
        }

        /// <summary>
        /// Stop listening
        /// </summary>
        public virtual void Stop()
        {
            if (!_started)
            {
                return;
            }
            _started = false;

            LogStop();
            _server?.Stop();
            _logger.Information("Stopped");
        }

        protected virtual Task<Stream> GetClientStream(TcpClient client)
        {
            return Task.FromResult<Stream>(client.GetStream());
        }

        protected virtual void LogStart()
        {
            _logger.Information("Starting ldap server on {endpoint}", GetLocalEndpoint());
        }

        protected virtual void LogStop()
        {
            _logger.Information("Stopping ldap server on {endpoint}", GetLocalEndpoint());
        }

        protected abstract IPEndPoint GetLocalEndpoint();

        private static async Task<Stream> GetServerStream(TcpClient serverConnection, 
            RemoteEndPoint remoteEndPoint)
        {
            var serverStream = serverConnection.GetStream();

            if (!remoteEndPoint.UseTls)
            {
                return serverStream;
            }

            var sslSream = new SslStream(serverStream, false, new RemoteCertificateValidationCallback(ValidateServerCertificate), null);
            await sslSream.AuthenticateAsClientAsync(remoteEndPoint.Host);

            return sslSream;
        }

        private static bool ValidateServerCertificate(object sender,
            X509Certificate certificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyErrors) => true;

        /// <summary>
        /// Start the loop used for accepting clients
        /// </summary>
        private async Task Receive()
        {
            while (_server.Server.IsBound)
            {
                try
                {
                    var remoteClient = await _server.AcceptTcpClientAsync();
                    var task = Task.Factory.StartNew(async () => await HandleClient(remoteClient), TaskCreationOptions.LongRunning);
                }
                catch (ObjectDisposedException) //may be safetly ignored
                {

                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Something went wrong accepting client");
                }
            }
        }

        private async Task HandleClient(TcpClient client)
        {
            client.NoDelay = true;

            var clientEndpoint = (IPEndPoint)client.Client.RemoteEndPoint;
            var clientConfiguration = _serviceConfiguration.GetClient(clientEndpoint.Address);

            if (clientConfiguration == null)
            {
                _logger.Warning("Received packet from unknown client {host:l}:{port}, closing", clientEndpoint.Address, clientEndpoint.Port);
                client.Close();
                return;
            }

            try
            {
                foreach (var ldapServer in clientConfiguration.SplittedLdapServers)
                {
                    var remoteEndPoint = ParseServerEndpoint(ldapServer.ToLower());
                    var isSuccessful = await ProcessRemoteEndPoint(remoteEndPoint, client, clientConfiguration);
                    if (isSuccessful)
                        return;
                }
            }
            finally
            {
                if (client.Connected)
                {
                    client.Close();
                }
            }
        }

        private async Task<bool> ProcessRemoteEndPoint(RemoteEndPoint remoteEndPoint, TcpClient client, ClientConfiguration clientConfiguration)
        {
            try
            {
                var serverEndpoint = remoteEndPoint.GetIPEndPoint();
                using var serverConnection = new TcpClient();
                await serverConnection.ConnectAsync(serverEndpoint.Address, serverEndpoint.Port);
                using var serverStream = await GetServerStream(serverConnection, remoteEndPoint);
                using var clientStream = await GetClientStream(client);
                
                var proxy = _proxyFactory.CreateProxy(
                    client,
                    clientStream, 
                    serverConnection,
                    serverStream,
                    clientConfiguration);
                
                await proxy.ProcessDataExchange();
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error while connecting client '{clientConfiguration.Name}' to {remoteEndPoint.Host}:{remoteEndPoint.Port}");
            }

            return false;
        }

        private static RemoteEndPoint ParseServerEndpoint(string server)
        {
            var remoteEndPoint = new RemoteEndPoint
            {
                Port = 389
            };

            if (server.StartsWith("ldaps://"))
            {
                remoteEndPoint.Port = 636;
                remoteEndPoint.UseTls = true;
                server = server.Substring(8);
            }

            if (server.StartsWith("ldap://"))
            {
                server = server[7..];
            }

            var parts = server.Split(':');

            remoteEndPoint.Host = parts[0];

            if (parts.Length > 1)
            {
                remoteEndPoint.Port = int.Parse(parts[1]);
            }

            return remoteEndPoint;
        }
    }
}