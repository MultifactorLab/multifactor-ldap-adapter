using MultiFactor.Ldap.Adapter.Configuration;
using MultiFactor.Ldap.Adapter.Services;
using System.Collections.Generic;
using System;
using Serilog;
using System.Linq;

namespace MultiFactor.Ldap.Adapter.Server
{
    public class LdapServersFactory
    {
        private readonly ServiceConfiguration _serviceConfiguration;
        private readonly LdapProxyFactory _proxyFactory;
        private readonly ILogger _logger;

        public LdapServersFactory(ServiceConfiguration serviceConfiguration, LdapProxyFactory proxyFactory, ILogger logger)
        {
            _serviceConfiguration = serviceConfiguration ?? throw new ArgumentNullException(nameof(serviceConfiguration));
            _proxyFactory = proxyFactory;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public IReadOnlyList<LdapServer> CreateServers() => GetServers().ToList().AsReadOnly();

        private IEnumerable<LdapServer> GetServers()
        {
            if (_serviceConfiguration.ServerConfig.AdapterLdapEndpoint != null)
            {
                yield return new LdapServer(_serviceConfiguration.ServerConfig.AdapterLdapEndpoint,
                    _serviceConfiguration,
                    _proxyFactory,
                    _logger);
            }
            if (_serviceConfiguration.ServerConfig.AdapterLdapsEndpoint != null)
            {
                yield return new LdapsServer(_serviceConfiguration.ServerConfig.AdapterLdapsEndpoint,
                    _serviceConfiguration,
                    _proxyFactory,
                    _logger);
            }
        }
    }
}
