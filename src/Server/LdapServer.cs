//Copyright(c) 2021 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-ldap-adapter/blob/main/LICENSE.md

using MultiFactor.Ldap.Adapter.Configuration;
using Serilog;
using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace MultiFactor.Ldap.Adapter.Server
{
    public class LdapServer
    {
        private TcpListener _server;
        protected ILogger _logger;
        protected IPEndPoint _localEndpoint;
        protected ServiceConfiguration _serviceConfiguration;

        public LdapServer(IPEndPoint localEndpoint, ServiceConfiguration serviceConfiguration, ILogger logger)
        {
            _localEndpoint = localEndpoint ?? throw new ArgumentNullException(nameof(localEndpoint));
            _serviceConfiguration = serviceConfiguration ?? throw new ArgumentNullException(nameof(serviceConfiguration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Start listening for requests
        /// </summary>
        public void Start()
        {
            _server = new TcpListener(_localEndpoint);

            LogStart();

            _server.Start();

            var receiveTask = Receive();

            _logger.Information("Server started");
        }

        /// <summary>
        /// Stop listening
        /// </summary>
        public void Stop()
        {
            LogStop();
            
            _server.Stop();
            
            _logger.Information("Stopped");
        }

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
                    var task = Task.Factory.StartNew(async () => await HandleClint(remoteClient), TaskCreationOptions.LongRunning);
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

        private async Task HandleClint(TcpClient client)
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

            var remoteEndPoint = ParseServerEndpoint(clientConfiguration.LdapServer.ToLower());

            try
            {
                var serverEndpoint = remoteEndPoint.GetIPEndPoint();

                using (var serverConnection = new TcpClient())
                {
                    await serverConnection.ConnectAsync(serverEndpoint.Address, serverEndpoint.Port);

                    using (var serverStream = await GetServerStream(serverConnection, remoteEndPoint))
                    {
                        using (var clientStream = await GetClientStream(client))
                        {
                            var proxy = new LdapProxy(client, clientStream, serverConnection, serverStream, _serviceConfiguration, clientConfiguration, _logger);
                            await proxy.Start();
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                _logger.Error(ex, $"Error while connecting client '{clientConfiguration.Name}' to {remoteEndPoint.Host}:{remoteEndPoint.Port}");
            }
            finally
            {
                if (client.Connected)
                {
                    client.Close();
                }
            }
        }

        public static bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        protected virtual async Task<Stream> GetClientStream(TcpClient client)
        {
            return await Task.FromResult(client.GetStream());
        }

        protected virtual void LogStart()
        {
            _logger.Information($"Starting ldap server on {_localEndpoint}");
        }

        protected virtual void LogStop()
        {
            _logger.Information($"Stopping ldap server on {_localEndpoint}");
        }

        private RemoteEndPoint ParseServerEndpoint(string server)
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
                server = server.Substring(7); 
            }

            var parts = server.Split(':');

            remoteEndPoint.Host = parts[0];

            if (parts.Length > 1)
            {
                remoteEndPoint.Port = int.Parse(parts[1]);
            }

            return remoteEndPoint;
        }

        private async Task<Stream> GetServerStream(TcpClient serverConnection, RemoteEndPoint remoteEndPoint)
        {
            var serverStream = serverConnection.GetStream();

            if (remoteEndPoint.UseTls)
            {
                var sslSream = new SslStream(serverStream, false, new RemoteCertificateValidationCallback(ValidateServerCertificate), null);
                await sslSream.AuthenticateAsClientAsync(remoteEndPoint.Host);

                return sslSream;
            }

            return serverStream;
        }
    }
}