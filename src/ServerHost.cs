using Microsoft.Extensions.Hosting;
using MultiFactor.Ldap.Adapter.Core;
using MultiFactor.Ldap.Adapter.Server;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MultiFactor.Ldap.Adapter
{
    internal class ServerHost : IHostedService
    {
        private Task _executingTask;
        private readonly CancellationTokenSource _stoppingCts = new CancellationTokenSource();

        private ILogger _logger;
        private readonly IEnumerable<ILdapServer> _ldapServers;
        private readonly ApplicationVariables _variables;

        public ServerHost(ILogger logger, IEnumerable<ILdapServer> ldapServers,
            ApplicationVariables variables)
        {
            _logger = logger;
            _ldapServers = ldapServers;
            _variables = variables;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.Information("Multifactor (c) LDAP Adapter, v. {Version:l}",
                _variables.AppVersion);

            foreach (var server in _ldapServers.Where(x => x.Enabled))
            {
                server.Start();
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
                foreach (var server in _ldapServers)
                {
                    server.Stop();
                }

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
                await Task.WhenAny(_executingTask, Task.Delay(Timeout.Infinite, cancellationToken));
            }
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
