//Copyright(c) 2021 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-ldap-adapter/blob/main/LICENSE.md

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
        protected RemoteEndPoint _remoteEndPoint;
        protected Configuration _configuration;

        public LdapServer(IPEndPoint localEndpoint, Configuration configuration, ILogger logger)
        {
            _localEndpoint = localEndpoint ?? throw new ArgumentNullException(nameof(localEndpoint));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _remoteEndPoint = ParseServerEndpoint(_configuration.LdapServer.ToLower());
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

            try
            {
                var serverEndpoint = _remoteEndPoint.GetIPEndPoint();

                using (var serverConnection = new TcpClient())
                {
                    await serverConnection.ConnectAsync(serverEndpoint.Address, serverEndpoint.Port);

                    using (var serverStream = await GetServerStream(serverConnection))
                    {
                        using (var clientStream = await GetClientStream(client))
                        {
                            var proxy = new LdapProxy(client, clientStream, serverConnection, serverStream, _configuration, _logger);
                            await proxy.Start();
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                _logger.Error(ex, "Connection error");
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

        private async Task<Stream> GetServerStream(TcpClient serverConnection)
        {
            var serverStream = serverConnection.GetStream();

            if (_remoteEndPoint.UseTls)
            {
                var sslSream = new SslStream(serverStream, false, new RemoteCertificateValidationCallback(ValidateServerCertificate), null);
                await sslSream.AuthenticateAsClientAsync(_remoteEndPoint.Host);

                return sslSream;
            }

            return serverStream;
        }
    }
}