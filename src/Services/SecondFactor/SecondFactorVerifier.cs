using MultiFactor.Ldap.Adapter.Configuration;
using MultiFactor.Ldap.Adapter.Services.Caching;
using MultiFactor.Ldap.Adapter.Services.MultiFactorApi;
using Serilog;
using System;
using System.Text;
using System.Threading.Tasks;

namespace MultiFactor.Ldap.Adapter.Services.SecondFactor
{
    public class SecondFactorVerifier
    {
        private readonly ServiceConfiguration _configuration;
        private readonly AuthenticatedClientCache _clientCache;
        private readonly MultiFactorApiClient _apiClient;
        private readonly ILogger _logger;

        public SecondFactorVerifier(
            ServiceConfiguration configuration,
            AuthenticatedClientCache clientCache,
            MultiFactorApiClient apiClient,
            ILogger logger)
        {
            _configuration = configuration;
            _clientCache = clientCache;
            _logger = logger;
            _apiClient = apiClient;
        }

        public async Task<bool> Authenticate(ConnectedClientInfo connectedClient)
        {
            //try to get authenticated client to bypass second factor if configured
            if (_clientCache.TryHitCache(connectedClient.ClientConfiguration.AuthenticationCacheLifetime, connectedClient.Username, connectedClient.ClientConfiguration.Name))
            {
                _logger.Information("Bypass second factor for user '{user:l}'", connectedClient.Username);
                return true;
            }

            var request = new MultiFactorAccessRequest
            {
                ApiUrls = _configuration.ApiUrls,
                Identity = connectedClient.Username,
                Auth = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{connectedClient.ClientConfiguration.MultifactorApiKey}:{connectedClient.ClientConfiguration.MultifactorApiSecret}")),
                BypassSecondFactor = connectedClient.ClientConfiguration.BypassSecondFactorWhenApiUnreachable
            };

            var response = await _apiClient.FaultTolerantSecondFactorRequest(request);

            if (response == MultiFactorAccessResponse.Empty)
            {
                return false;
            }

            if (response.Granted && !response.Bypassed)
            {
                _logger.Information("Second factor for user '{user:l}' verified successfully. Authenticator '{authenticator:l}', account '{account:l}'",
                    connectedClient.Username, response?.Authenticator, response?.Account);
                _clientCache.SetCache(connectedClient.ClientConfiguration.AuthenticationCacheLifetime, connectedClient.Username, connectedClient.ClientConfiguration.Name);
            }

            if (response.Denied)
            {
                var reason = response?.ReplyMessage;
                var phone = response?.Phone;
                _logger.Warning("Second factor verification for user '{user:l}' failed with reason='{reason:l}'. User phone {phone:l}",
                    connectedClient.Username, reason, phone);
            }

            return response.Granted;
        }
    }
}
