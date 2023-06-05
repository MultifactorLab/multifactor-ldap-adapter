//Copyright(c) 2021 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-ldap-adapter/blob/main/LICENSE.md

using MultiFactor.Ldap.Adapter.Core;
using Serilog;
using System;
using System.Linq;
using System.Text;

namespace MultiFactor.Ldap.Adapter.Server.Authentication
{
    public class NtlmBindAuthentication : BindAuthentication
    {
        public NtlmBindAuthentication(ILogger logger) : base(logger)
        {
        }

        public override string MechanismName => "NTLM";

        protected virtual byte[] GetAuthPacket(LdapAttribute bindRequest)
        {
            return bindRequest.ChildAttributes[2].Value;
        }

        public override string Parse(LdapAttribute bindRequest)
        {
            //https://docs.microsoft.com/en-us/openspecs/windows_protocols/ms-nlmp/760a9788-bd32-4d9e-87ad-2aa5970786ac
            
            var authPacket = GetAuthPacket(bindRequest);

            var messageType = authPacket[8];
            switch (messageType)
            {
                case 1:
                case 2:
                    //negotiate/challenge
                    break;
                case 3:
                    //authentication
                    var userNameFieldsData = authPacket.Skip(36).Take(8).ToArray();
                    var userNameLen = BitConverter.ToUInt16(userNameFieldsData.Take(2).ToArray(), 0);
                    var userNameOffset = BitConverter.ToUInt16(userNameFieldsData.Skip(4).Take(4).ToArray(), 0);

                    var userNameData = authPacket.Skip(userNameOffset).Take(userNameLen).ToArray();
                    return Encoding.Unicode.GetString(userNameData);
                default:
                    throw new InvalidOperationException($"Unknown NTLMSSP message type: {messageType}");
            }

            return null;
        }

        public override void WriteUsername(LdapAttribute bindRequest, string username)
        {
        }
    }
}