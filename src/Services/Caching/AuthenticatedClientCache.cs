using MultiFactor.Ldap.Adapter.Configuration;
using System.Collections.Concurrent;
using System;
using Serilog;

namespace MultiFactor.Ldap.Adapter.Services.Caching
{
    public class AuthenticatedClientCache
    {
        private static readonly ConcurrentDictionary<string, AuthenticatedClient> _authenticatedClients = new ConcurrentDictionary<string, AuthenticatedClient>();
        private readonly ILogger _logger;

        public AuthenticatedClientCache(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public bool TryHitCache(string userName, ClientConfiguration clientConfiguration)
        {
            if (!clientConfiguration.AuthenticationCacheLifetime.Enabled) return false;

            if (string.IsNullOrEmpty(userName))
            {
                _logger.Warning($"Username miss");
                return false;
            }

            var id = AuthenticatedClient.ParseId(clientConfiguration.Name, userName);
            if (!_authenticatedClients.TryGetValue(id, out var authenticatedClient))
            {
                return false;
            }

            _logger.Debug($"User {userName} authenticated {authenticatedClient.Elapsed.ToString("hh\\:mm\\:ss")} ago. Authentication session period: {clientConfiguration.AuthenticationCacheLifetime.Lifetime}");

            if (authenticatedClient.Elapsed <= clientConfiguration.AuthenticationCacheLifetime.Lifetime)
            {
                return true;
            }

            _authenticatedClients.TryRemove(id, out _);

            return false;
        }

        public void SetCache(string userName, ClientConfiguration clientConfiguration)
        {
            if (!clientConfiguration.AuthenticationCacheLifetime.Enabled || string.IsNullOrEmpty(userName)) return;

            var client = AuthenticatedClient.Create(clientConfiguration.Name, userName);
            if (!_authenticatedClients.ContainsKey(client.Id))
            {
                _authenticatedClients.TryAdd(client.Id, client);
            }
        }
    }
}
