//Copyright(c) 2021 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-ldap-adapter/blob/main/LICENSE.md

using MultiFactor.Ldap.Adapter.Configuration;
using MultiFactor.Ldap.Adapter.Services;
using Serilog;
using System.Net;

namespace MultiFactor.Ldap.Adapter.Server
{
    internal class LdapServer : LdapServerBase, ILdapServer
    {
        public LdapServer(ServiceConfiguration serviceConfiguration,
            LdapProxyFactory proxyFactory,
            ILogger logger) : base(serviceConfiguration, proxyFactory, logger) { }

        public bool Enabled => _serviceConfiguration.ServerConfig.AdapterLdapEndpoint != null;

        protected override IPEndPoint GetLocalEndpoint() => _serviceConfiguration.ServerConfig.AdapterLdapEndpoint;
    }
}