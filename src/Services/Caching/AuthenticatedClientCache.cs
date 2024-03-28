using MultiFactor.Ldap.Adapter.Configuration;
using Serilog;
using System;
using System.Collections.Concurrent;

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

        public bool TryHitCache(AuthenticatedClientCacheConfig cacheConfig, string userName, string clientName)
        {
            if (!cacheConfig.Enabled) return false;

            if (string.IsNullOrEmpty(userName))
            {
                _logger.Warning($"Username miss");
                return false;
            }

            var id = AuthenticatedClient.ParseId(clientName, userName);
            if (!_authenticatedClients.TryGetValue(id, out var authenticatedClient))
            {
                return false;
            }

            _logger.Debug($"User {userName} authenticated {authenticatedClient.Elapsed:hh\\:mm\\:ss} ago. Authentication session period: {cacheConfig.Lifetime}");

            if (authenticatedClient.Elapsed <= cacheConfig.Lifetime)
            {
                return true;
            }

            _authenticatedClients.TryRemove(id, out _);

            return false;
        }

        public void SetCache(AuthenticatedClientCacheConfig cacheConfig, string userName, string clientName)
        {
            if (!cacheConfig.Enabled || string.IsNullOrEmpty(userName)) return;

            var client = AuthenticatedClient.Create(clientName, userName);
            if (!_authenticatedClients.ContainsKey(client.Id))
            {
                _authenticatedClients.TryAdd(client.Id, client);
            }
        }
    }
}
