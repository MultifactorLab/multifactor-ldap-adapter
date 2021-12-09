//Copyright(c) 2021 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-ldap-adapter/blob/main/LICENSE.md

using MultiFactor.Ldap.Adapter.Core;
using MultiFactor.Ldap.Adapter.Server.Authentication;
using MultiFactor.Ldap.Adapter.Services;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MultiFactor.Ldap.Adapter.Server
{
    public class LdapProxy
    {
        private TcpClient _clientConnection;
        private TcpClient _serverConnection;
        private Stream _clientStream;
        private Stream _serverStream;
        private Configuration _configuration;
        private ILogger _logger;
        private string _userName;
        private string _lookupUserName;

        private LdapProxyAuthenticationStatus _status;

        private static readonly ConcurrentDictionary<string, string> _usersDn = new ConcurrentDictionary<string, string>();

        public LdapProxy(TcpClient clientConnection, Stream clientStream, TcpClient serverConnection, Stream serverStream, Configuration configuration, ILogger logger)
        {
            _clientConnection = clientConnection ?? throw new ArgumentNullException(nameof(clientConnection));
            _clientStream = clientStream ?? throw new ArgumentNullException(nameof(clientStream));
            _serverConnection = serverConnection ?? throw new ArgumentNullException(nameof(serverConnection));
            _serverStream = serverStream ?? throw new ArgumentNullException(nameof(serverStream));

            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task Start()
        {
            var from = _clientConnection.Client.RemoteEndPoint.ToString();
            var to = _serverConnection.Client.RemoteEndPoint.ToString();

            _logger.Debug("Opened {client} => {server}", from, to);

            await Task.WhenAny(
                DataExchange(_clientConnection, _clientStream, _serverConnection, _serverStream, ParseAndProcessRequest),
                DataExchange(_serverConnection, _serverStream, _clientConnection, _clientStream, ParseAndProcessResponse));

            _logger.Debug("Closed {client} => {server}", from, to);
        }

        private async Task DataExchange(TcpClient source, Stream sourceStream, TcpClient target, Stream targetStream, Func<byte[], int, (byte[], int)> process)
        {
            try
            {
                var bytesRead = 0;
                var requestData = new byte[8192];

                do
                {
                    //read
                    bytesRead = await sourceStream.ReadAsync(requestData, 0, requestData.Length);

                    //process
                    var response = process(requestData, bytesRead);

                    //write
                    await targetStream.WriteAsync(response.Item1, 0, response.Item2);

                    if (_status == LdapProxyAuthenticationStatus.AuthenticationFailed)
                    {
                        source.Close();
                    }

                } while (bytesRead != 0);
            }
            catch(IOException)
            {
                //connection closed unexpectly
                //_logger.Debug(ioex, "proxy");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Data exchange error from {client} to {server}", source.Client.RemoteEndPoint, target.Client.RemoteEndPoint);
            }
        }

        private (byte[], int) ParseAndProcessRequest(byte[] data, int length)
        {
            var packet = LdapPacket.ParsePacket(data);

            var searchRequest = packet.ChildAttributes.SingleOrDefault(c => c.LdapOperation == LdapOperation.SearchRequest);
            if (searchRequest != null)
            {
                var filter = searchRequest.ChildAttributes[6];
                var user = SearchUserName(filter);

                if (!string.IsNullOrEmpty(user))
                {
                    _status = LdapProxyAuthenticationStatus.UserDnSearch;
                    _lookupUserName = user;
                }
            }

            var bindRequest = packet.ChildAttributes.SingleOrDefault(c => c.LdapOperation == LdapOperation.BindRequest);
            if (bindRequest != null)
            {
                var authentication = LoadAuthentication(bindRequest);
                if (authentication != null && authentication.TryParse(bindRequest, out var userName))
                {
                    if (!string.IsNullOrEmpty(userName)) //empty userName means anonymous bind
                    {
                        if (IsServiceAccount(userName))
                        {
                            //service acc
                            _logger.Debug($"Received {authentication.MechanismName} bind request for service account '{{user:l}}' from {{client}}", userName, _clientConnection.Client.RemoteEndPoint);
                        }
                        else
                        {
                            //user acc
                            _userName = ConvertDistinguishedNameToUserName(userName);
                            _status = LdapProxyAuthenticationStatus.BindRequested;
                            _logger.Debug($"Received {authentication.MechanismName} bind request for user '{{user:l}}' from {{client}}", userName, _clientConnection.Client.RemoteEndPoint);
                        }
                    }
                }
            }

            return (data, length);
        }

        private (byte[], int) ParseAndProcessResponse(byte[] data, int length)
        {
            if (_status == LdapProxyAuthenticationStatus.BindRequested)
            {
                var packet = LdapPacket.ParsePacket(data);
                var bindResponse = packet.ChildAttributes.SingleOrDefault(c => c.LdapOperation == LdapOperation.BindResponse);
            
                if (bindResponse != null)
                {
                    var bindResult = bindResponse.ChildAttributes[0].GetValue<LdapResult>();
                    if (bindResult == LdapResult.saslBindInProgress)    //  challenge/response in process
                    {
                        return (data, length);  //just proxy
                    }

                    var bound = bindResult == LdapResult.success;

                    if (bound)  //first factor authenticated
                    {
                        _logger.Information("User '{user:l}' credential verified successfully at {server}", _userName, _serverConnection.Client.RemoteEndPoint);

                        var apiClient = new MultiFactorApiClient(_configuration, _logger);
                        var result = apiClient.Authenticate(_userName); //second factor

                        if (!result) // second factor failed
                        {
                            //return invalid creds response
                            var responsePacket = InvalidCredentials(packet);
                            var response = responsePacket.GetBytes();

                            _logger.Debug("Sent invalid credential response for user '{user:l}' to {client}", _userName, _clientConnection.Client.RemoteEndPoint);

                            _status = LdapProxyAuthenticationStatus.AuthenticationFailed;

                            return (response, response.Length);
                        }

                        _status = LdapProxyAuthenticationStatus.None;
                    }
                    else //first factor authentication failed
                    {
                        //just log
                        var reason = bindResponse.ChildAttributes[2].GetValue<string>();
                        _logger.Warning("Verification user '{user:l}' at {server} failed: {reason}", _userName, _serverConnection.Client.RemoteEndPoint, reason);
                    }
                }
            }

            if (_status == LdapProxyAuthenticationStatus.UserDnSearch)
            {
                var packet = LdapPacket.ParsePacket(data);
                var searchResultEntry = packet.ChildAttributes.SingleOrDefault(c => c.LdapOperation == LdapOperation.SearchResultEntry);
                
                if (searchResultEntry != null)
                {
                    var userDn = searchResultEntry.ChildAttributes[0].GetValue<string>();

                    if (_lookupUserName != null && userDn != null)
                    {
                        userDn = userDn.ToLower(); //becouse some apps do it
                        
                        _usersDn.TryRemove(userDn, out _);
                        _usersDn.TryAdd(userDn, _lookupUserName);
                    }
                }

                _status = LdapProxyAuthenticationStatus.None;
            }

            return (data, length);  //just proxy
        }

        private string ConvertDistinguishedNameToUserName(string dn)
        {
            if (string.IsNullOrEmpty(dn)) return dn;

            dn = dn.ToLower();

            if (_usersDn.TryGetValue(dn, out var userName))
            {
                return userName;
            }

            return dn;
        }

        private BindAuthentication LoadAuthentication(LdapAttribute bindRequest)
        {
            var authPacket = bindRequest.ChildAttributes[2];
            if (!authPacket.IsConstructed)  //simple bind or NTLM
            {
                if (BindAuthentication.IsNtlm(authPacket.Value))
                {
                    return new NtlmBindAuthentication(_logger);
                }

                return new SimpleBindAuthentication(_logger);
            }

            var mechanism = authPacket.ChildAttributes[0].GetValue<string>();
            
            if (mechanism == "DIGEST-MD5")
            {
                return new DigestMd5BindAuthentication(_logger);
            }
            
            if (mechanism == "GSS-SPNEGO")
            {
                var saslPacket = authPacket.ChildAttributes[1].Value;
                if (BindAuthentication.IsNtlm(saslPacket))
                {
                    return new SpnegoNtlmBindAuthentication(_logger);
                }
            }

            //kerberos or not-implemented
            //_logger.Debug($"Unknown bind mechanism: {mechanism}");

            return null;
        }

        private string SearchUserName(LdapAttribute attr)
        {
            var userNameAttrs = new[] { "cn", "uid", "samaccountname", "userprincipalname" };
            var contextType = (LdapFilterChoice)attr.ContextType;

            if (contextType == LdapFilterChoice.equalityMatch)
            {
                var left = attr.ChildAttributes[0].GetValue<string>()?.ToLower();
                var right = attr.ChildAttributes[1].GetValue<string>();

                if (userNameAttrs.Any(cn => cn == left))
                {
                    //user name lookup, from login to DN
                    return right;
                }
            }
            if (contextType == LdapFilterChoice.and || contextType == LdapFilterChoice.or)
            {
                foreach (var child in attr.ChildAttributes)
                {
                    var userName = SearchUserName(child);
                    if (userName != null) return userName;
                }
            }

            return null;
        }

        private LdapPacket InvalidCredentials(LdapPacket requestPacket)
        {
            var responsePacket = new LdapPacket(requestPacket.MessageId);
            responsePacket.ChildAttributes.Add(new LdapResultAttribute(LdapOperation.BindResponse, LdapResult.invalidCredentials));
            return responsePacket;
        }

        private bool IsServiceAccount(string userName)
        {
            if (_configuration.ServiceAccounts.Any(acc => acc == userName.ToLower()))
            {
                return true;
            }

            if (_configuration.ServiceAccountsOrganizationUnit != null)
            {
                if (_configuration.ServiceAccountsOrganizationUnit.Any(ou => userName.Contains(ou, StringComparison.InvariantCultureIgnoreCase)))
                {
                    return true;
                }
            }

            return false;
        }
    }

    public enum LdapProxyAuthenticationStatus
    {
        None,
        UserDnSearch,
        BindRequested,
        AuthenticationFailed
    }
}