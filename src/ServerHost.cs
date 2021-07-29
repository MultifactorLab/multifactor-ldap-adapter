using Microsoft.Extensions.Hosting;
using MultiFactor.Ldap.Adapter.Server;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MultiFactor.Ldap.Adapter
{
    public class ServerHost : IHostedService
    {
        private Task _executingTask;
        private readonly CancellationTokenSource _stoppingCts = new CancellationTokenSource();

        private ILogger _logger;
        private Configuration _configuration;

        private LdapServer _ldapServer;
        private LdapsServer _ldapsServer;

        public ServerHost(ILogger logger, Configuration configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            if (_configuration.StartLdapServer)
            {
                _ldapServer = new LdapServer(_configuration.AdapterLdapEndpoint, _configuration, logger);
            }
            if (_configuration.StartLdapsServer)
            {
                _ldapsServer = new LdapsServer(_configuration.AdapterLdapsEndpoint, _configuration, logger);
            }
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (_configuration.StartLdapServer)
            {
                _ldapServer.Start();
            }

            if (_configuration.StartLdapsServer)
            {
                _ldapsServer.Start();
            }

            // Store the task we're executing
            _executingTask = ExecuteAsync(_stoppingCts.Token);

            // If the task is completed then return it,
            // this will bubble cancellation and failure to the caller
            if (_executingTask.IsCompleted)
            {
                return _executingTask;
            }

            // Otherwise it's running
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            // Stop called without start
            if (_executingTask == null)
            {
                return;
            }

            try
            {
                _ldapServer?.Stop();
                _ldapsServer?.Stop();
                
                // Signal cancellation to the executing method
                _stoppingCts.Cancel();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "StopAsync");
            }
            finally
            {
                // Wait until the task completes or the stop token triggers
                await Task.WhenAny(_executingTask, Task.Delay(Timeout.Infinite,
                                                          cancellationToken));             }
        }

        protected async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                //infinite job
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
        }
    }
}
