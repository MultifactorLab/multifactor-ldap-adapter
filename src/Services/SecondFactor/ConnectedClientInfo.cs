//Copyright(c) 2021 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-ldap-adapter/blob/main/LICENSE.md

using MultiFactor.Ldap.Adapter.Configuration;
using System;

namespace MultiFactor.Ldap.Adapter.Services.SecondFactor
{
    public class ConnectedClientInfo
    {
        public string Username { get; }
        public ClientConfiguration ClientConfiguration { get; }

        public ConnectedClientInfo(string username, ClientConfiguration clientConfiguration)
        {
            if (string.IsNullOrEmpty(username))
            {
                throw new ArgumentException($"'{nameof(username)}' cannot be null or empty.", nameof(username));
            }

            Username = username;
            ClientConfiguration = clientConfiguration ?? throw new ArgumentNullException(nameof(clientConfiguration));
        }
    }
}
