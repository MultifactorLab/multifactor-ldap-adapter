//Copyright(c) 2021 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-ldap-adapter/blob/main/LICENSE.md

using MultiFactor.Ldap.Adapter.Configuration;
using MultiFactor.Ldap.Adapter.Core;
using MultiFactor.Ldap.Adapter.Core.Requests;
using MultiFactor.Ldap.Adapter.Server.Authentication;
using MultiFactor.Ldap.Adapter.Server.LdapPacketModifiers;
using MultiFactor.Ldap.Adapter.Server.LdapStream;
using MultiFactor.Ldap.Adapter.Services;
using MultiFactor.Ldap.Adapter.Services.SecondFactor;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace MultiFactor.Ldap.Adapter.Server
{
    public class LdapProxy
    {
        private readonly TcpClient _clientConnection;
        private readonly TcpClient _serverConnection;
        private readonly Stream _clientStream;
        private readonly Stream _serverStream;
        private readonly SecondFactorVerifier _secondFactorVerifier;
        private readonly ClientConfiguration _clientConfig;
        private readonly ILogger _logger;
        private string _userName;
        private string _lookupUserName;
        private string _transformedUserName;

        private readonly LdapService _ldapService;

        private LdapProxyAuthenticationStatus _status;

        private static readonly ConcurrentDictionary<string, string> _usersDn2Cn = new ConcurrentDictionary<string, string>();
        private static readonly ConcurrentDictionary<string, string> _usersCn2Dn = new ConcurrentDictionary<string, string>();

        private readonly RandomWaiter _waiter;

        public LdapProxy(
            TcpClient clientConnection, 
            Stream clientStream,
            TcpClient serverConnection,
            Stream serverStream, 
            ClientConfiguration clientConfig,
            SecondFactorVerifier secondFactorVerifier,
            RandomWaiter waiter, 
            ILogger logger)
        {
            _clientConnection = clientConnection ?? throw new ArgumentNullException(nameof(clientConnection));
            _clientStream = clientStream ?? throw new ArgumentNullException(nameof(clientStream));
            _serverConnection = serverConnection ?? throw new ArgumentNullException(nameof(serverConnection));
            _serverStream = serverStream ?? throw new ArgumentNullException(nameof(serverStream));

            _secondFactorVerifier = secondFactorVerifier ?? throw new ArgumentNullException(nameof(secondFactorVerifier));
            _waiter = waiter ?? throw new ArgumentNullException(nameof(waiter));
            _clientConfig = clientConfig ?? throw new ArgumentNullException(nameof(clientConfig));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _ldapService = new LdapService(clientConfig);
        }

        public async Task Start()
        {
            var from = _clientConnection.Client.RemoteEndPoint.ToString();
            var to = _serverConnection.Client.RemoteEndPoint.ToString();

            _logger.Information("Opened {client} => {server} client {clientName:l}", from, to, _clientConfig.Name);

            await Task.WhenAny(
                DataExchange(_clientConnection, _clientStream, _serverConnection, _serverStream, ParseAndProcessRequest),
                DataExchange(_serverConnection, _serverStream, _clientConnection, _clientStream, ParseAndProcessResponse));

            _logger.Debug("Closed {client} => {server} client {clientName:l}", from, to, _clientConfig.Name);
        }

        private async Task DataExchange(TcpClient source, Stream sourceStream, TcpClient target, Stream targetStream, Func<byte[], int, Task<(byte[], int)>> process)
        {
            try
            {
                var streamReader = new LdapStreamReader(sourceStream);
                LdapPacketBuffer ldapPacket;
                do
                {
                    //read packet
                    ldapPacket = await streamReader.ReadLdapPacket();
                    if (ldapPacket.Data.Length == 0)
                    {
                        break;
                    }
                    if (!ldapPacket.PacketValid)
                    {
                        // bypass data
                        await targetStream.WriteAsync(ldapPacket.Data, 0, ldapPacket.Data.Length);
                        continue;
                    }
                   
                    //process
                    var response = await process(ldapPacket.Data, ldapPacket.Data.Length);
                    //write
                    await targetStream.WriteAsync(response.Item1, 0, response.Item2);

                    if (_status == LdapProxyAuthenticationStatus.AuthenticationFailed)
                    {
                        source.Close();
                    }
                } while (ldapPacket.Data.Length > 0);
            }
            catch (IOException)
            {
                //connection closed unexpectly
                //_logger.Debug(ioex, "proxy");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Data exchange error from {client} to {server}", source.Client.RemoteEndPoint, target.Client.RemoteEndPoint);
            }
        }

        private async Task<(byte[], int)> ParseAndProcessRequest(byte[] data, int length)
        {
            var request = await LdapRequest.FromBytesAsync(data);

            if (request.RequestType == LdapRequestType.SearchRequest)
            {
                var filter = request.As<SearchRequest>().SearchAttribute.ChildAttributes[6];
                var user = SearchUserName(filter);

                if (!string.IsNullOrEmpty(user))
                {
                    _status = LdapProxyAuthenticationStatus.UserDnSearch;
                    _lookupUserName = user;
                }
            } 

            if (request.RequestType == LdapRequestType.BindRequest)
            {
                var bindReq = request.As<BindRequest>();
                var authFactory = new BindAuthenticationFactory(_logger);
                var authentication = authFactory.GetAuthentication(bindReq.BindAttribute);
                if (authentication != null && authentication.TryParse(bindReq.BindAttribute, out var userName))
                {
                    if (!string.IsNullOrEmpty(userName)) //empty userName means anonymous bind
                    {
                        if (IsServiceAccount(userName))
                        {
                            //service acc
                            _logger.Debug($"Received {authentication.MechanismName} bind request for service account '{{user:l}}' from {{client}} {{clientName:l}}", userName, _clientConnection.Client.RemoteEndPoint, _clientConfig.Name);
                        }
                        else
                        {

                            //user acc
                            _userName = ConvertDistinguishedNameToCommonName(userName);
                            _status = LdapProxyAuthenticationStatus.BindRequested;
                            _logger.Information($"Received {authentication.MechanismName} bind request for user '{{user:l}}' from {{client}} {{clientName:l}}", _userName, _clientConnection.Client.RemoteEndPoint, _clientConfig.Name);

                            // To display a login message we need to save the transformation result
                            _transformedUserName = _userName;
                            if (_clientConfig.UserNameTransformRules.BeforeFirstFactor.Count != 0)
                            {
                                _transformedUserName = UserNameTransformer.ProcessUserNameTransformRules(_userName, _clientConfig.UserNameTransformRules.BeforeFirstFactor);
                            }

                            var modifier = RequestModifierFactory.CreateModifier<BindRequest>(_clientConfig, _logger);
                            var modifiedBytes = modifier.Modify(bindReq).Packet.GetBytes();
                            return await Task.FromResult((modifiedBytes, modifiedBytes.Length));
                        }
                    }
                }
            }

            return await Task.FromResult((data, length));
        }

        private async Task<(byte[], int)> ParseAndProcessResponse(byte[] data, int length)
        {
            if (_status == LdapProxyAuthenticationStatus.BindRequested)
            {
                var packet = await LdapPacket.ParsePacket(data);
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
                        _logger.Information("User '{user:l}' credential verified successfully at {server}", _transformedUserName, _serverConnection.Client.RemoteEndPoint);

                        var bypass = false;

                        //apply login transformation users if any
                        if(_clientConfig.UserNameTransformRules.BeforeSecondFactor.Count != 0)
                        {
                            _logger.Debug("Transform the username before second factor authentication."); ;
                            _userName = UserNameTransformer.ProcessUserNameTransformRules(_userName, _clientConfig.UserNameTransformRules.BeforeSecondFactor, _logger);
                        }

                        var profile = await _ldapService.LoadProfile(_serverStream, _userName);
                        var profileLoaded = profile != null;
                        if (!profileLoaded)
                        {
                            _logger.Error("User '{user:l}' not found. Can not check groups membership", _userName);
                        }
                        if (profileLoaded && _clientConfig.CheckUserGroups())
                        {
                            profile.MemberOf = await _ldapService.GetAllGroups(_serverStream, profile, _clientConfig);

                            //check ACL
                            if (_clientConfig.ActiveDirectoryGroup.Any())
                            {
                                var accessGroup = _clientConfig.ActiveDirectoryGroup.FirstOrDefault(group => IsMemberOf(profile, group));
                                if (accessGroup != null)
                                {
                                    _logger.Debug($"User '{{user:l}}' is member of '{accessGroup.Trim()}' access group in {profile.BaseDn}", _userName);
                                }
                                else
                                {
                                    _logger.Warning($"User '{{user:l}}' is not member of '{string.Join(';', _clientConfig.ActiveDirectoryGroup)}' access group in {profile.BaseDn}", _userName);

                                    //return invalid creds response
                                    var responsePacket = InvalidCredentials(packet);
                                    var response = responsePacket.GetBytes();

                                    _logger.Debug("Sent invalid credential response for user '{user:l}' to {client}", _userName, _clientConnection.Client.RemoteEndPoint);

                                    _status = LdapProxyAuthenticationStatus.AuthenticationFailed;

                                    return (response, response.Length);
                                }
                            }

                            //check if mfa is mandatory
                            if (_clientConfig.ActiveDirectory2FaGroup.Any())
                            {
                                var mfaGroup = _clientConfig.ActiveDirectory2FaGroup.FirstOrDefault(group => IsMemberOf(profile, group));
                                if (mfaGroup != null)
                                {
                                    _logger.Debug($"User '{{user:l}}' is member of '{mfaGroup.Trim()}' 2FA group in {profile.BaseDn}", _userName);
                                }
                                else
                                {
                                    _logger.Debug($"User '{{user:l}}' is not member of '{string.Join(';', _clientConfig.ActiveDirectory2FaGroup)}' 2FA group in {profile.BaseDn}", _userName);
                                    bypass = true;
                                }
                            }

                            //check of mfa is not mandatory
                            if (_clientConfig.ActiveDirectory2FaBypassGroup.Any() && !bypass)
                            {
                                var bypassGroup = _clientConfig.ActiveDirectory2FaBypassGroup.FirstOrDefault(group => IsMemberOf(profile, group));
                                if (bypassGroup != null)
                                {
                                    _logger.Information($"User '{{user:l}}' is member of '{bypassGroup.Trim()}' 2FA bypass group in {profile.BaseDn}", _userName);
                                    bypass = true;
                                }
                                else
                                {
                                    _logger.Debug($"User '{{user:l}}' is not member of '{string.Join(';', _clientConfig.ActiveDirectory2FaBypassGroup)}' 2FA bypass group in {profile.BaseDn}", _userName);
                                }
                            }
                        }

                        if (profileLoaded && !bypass)
                        {
                            if (LdapService.GetIdentityType(_userName) == IdentityType.DistinguishedName)   //user uses DN as login ;)
                            {
                                if (profile?.Uid == null)
                                {
                                    _logger.Warning($"Unable to transform DN to UID: profile is not loaded for '{{user:l}}' or profile UID is null", _userName);
                                }
                                else
                                {
                                    _userName = profile.Uid ?? _userName;
                                }
                            }
                            
                            var connectedClient = new ConnectedClientInfo(_userName, _clientConfig);
                            var result = await _secondFactorVerifier.Authenticate(connectedClient);

                            if (!result) // second factor failed
                            {
                                //return invalid creds response
                                var responsePacket = InvalidCredentials(packet);
                                var response = responsePacket.GetBytes();

                                _logger.Debug("Sent invalid credential response for user '{user:l}' to {client}", _userName, _clientConnection.Client.RemoteEndPoint);

                                _status = LdapProxyAuthenticationStatus.AuthenticationFailed;

                                return (response, response.Length);
                            }
                        }
                        else
                        {
                            _logger.Information("Bypass second factor for user '{user:l}'", _userName);
                        }

                        _status = LdapProxyAuthenticationStatus.None;
                    }
                    else //first factor authentication failed
                    {
                        //just log
                        var reason = bindResponse.ChildAttributes[2].GetValue<string>();
                        await _waiter.WaitSomeTimeAsync();
                        _logger.Warning("Verification user '{user:l}' at {server} failed: {reason}", _transformedUserName, _serverConnection.Client.RemoteEndPoint, reason);
                    }
                }
            }

            if (_status == LdapProxyAuthenticationStatus.UserDnSearch)
            {
                var packet = await LdapPacket.ParsePacket(data);
                var searchResultEntry = packet.ChildAttributes.SingleOrDefault(c => c.LdapOperation == LdapOperation.SearchResultEntry);

                if (searchResultEntry != null)
                {
                    var userDn = searchResultEntry.ChildAttributes[0].GetValue<string>();

                    if (_lookupUserName != null && userDn != null)
                    {
                        userDn = userDn.ToLower(); //becouse some apps do it

                        _usersDn2Cn.TryRemove(userDn, out _);
                        _usersDn2Cn.TryAdd(userDn, _lookupUserName);

                        _usersCn2Dn.TryRemove(_lookupUserName, out _);
                        _usersCn2Dn.TryAdd(_lookupUserName, userDn);
                    }
                }

                _status = LdapProxyAuthenticationStatus.None;
            }

            return (data, length);  //just proxy
        }

        private string ConvertDistinguishedNameToCommonName(string dn)
        {
            if (string.IsNullOrEmpty(dn)) return dn;

            dn = dn.ToLower();

            if (_usersDn2Cn.TryGetValue(dn, out var cn))
            {
                return cn;
            }

            return dn;
        }

        private string SearchUserName(LdapAttribute attr)
        {
            var userNameAttrs = new[] { "cn", "uid", "samaccountname", "userprincipalname" };
            var contextType = (LdapFilterChoice?)attr.ContextType;

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
            if (_clientConfig.ServiceAccounts.Any(acc => acc == userName.ToLower()))
            {
                return true;
            }

            if (_clientConfig.ServiceAccountsOrganizationUnit.Any(ou => userName.ToLower().Contains(ou)))
            {
                return true;
            }

            return false;
        }

        private bool IsMemberOf(LdapProfile profile, string group)
        {
            return profile.MemberOf?.Any(g => g.ToLower() == group.ToLower().Trim()) ?? false;
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