using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MultiFactor.Ldap.Adapter.Configuration;
using MultiFactor.Ldap.Adapter.Configuration.Core;
using MultiFactor.Ldap.Adapter.Core;
using MultiFactor.Ldap.Adapter.Core.Logging;
using MultiFactor.Ldap.Adapter.Core.NameResolve;
using MultiFactor.Ldap.Adapter.Server;
using MultiFactor.Ldap.Adapter.Services;
using MultiFactor.Ldap.Adapter.Services.Caching;
using Serilog;
using Serilog.Core;
using System;
using System.Text;

namespace MultiFactor.Ldap.Adapter
{
    public class Program
    {
        public static void Main(string[] args)
        {
            IHost host = null;

            try
            {
                host = Host
                    .CreateDefaultBuilder(args)
                    .ConfigureServices(services => ConfigureServices(services))
                    .Build();
                host.Run();
            }
            catch (Exception ex)
            {
                var errorMessage = FlattenException(ex);

                if (Log.Logger != null)
                {
                    Log.Logger.Error($"Unable to start: {errorMessage}");
                }
                else
                {
                    Console.WriteLine($"Unable to start: {errorMessage}");
                }

                host?.StopAsync();
            }

        }

        private static void ConfigureServices(IServiceCollection services)
        {
            var loggingLevelSwitch = new LoggingLevelSwitch();
            services.AddSingleton<ClientLoggerProvider>();
            services.AddSingleton(sp => {
                Log.Logger = LoggerFactory.CreateLogger(ServiceConfiguration.GetLogFormat(), ServiceConfiguration.GetLogLevel());
                return Log.Logger;
            });

            services.AddSingleton<IConfigurationProvider, ConfigurationProvider>();
            services.AddSingleton(sp => {
                var configurationProvider = sp.GetRequiredService<IConfigurationProvider>();
                var logger = sp.GetRequiredService<ILogger>();
                var serviceConf = new ServiceConfiguration(configurationProvider, logger);
                if (serviceConf.ServerConfig.AdapterLdapsEndpoint != null)
                {
                    TlsCertificateFactory.EnsureTlsCertificatesExist(Core.Constants.ApplicationPath, serviceConf, Log.Logger);
                }
                return serviceConf;
            });
            services.AddSingleton(prov => new RandomWaiter(prov.GetRequiredService<ServiceConfiguration>().InvalidCredentialDelay));
            services.AddSingleton<MultiFactorApiClient>();
            services.AddHttpClientWithProxy();
            services.AddSingleton<LdapProxyFactory>();
            services.AddSingleton<LdapServersFactory>();
            services.AddSingleton<AuthenticatedClientCache>();
            services.AddMemoryCache();
            services.AddSingleton(prov => prov.GetRequiredService<LdapServersFactory>().CreateServers());
            services.AddHostedService<ServerHost>();
            services.AddTransient<NameResolverService>();
        }

        private static string FlattenException(Exception exception)
        {
            var stringBuilder = new StringBuilder();

            var counter = 0;

            while (exception != null)
            {
                if (counter++ > 0)
                {
                    var prefix = new string('-', counter) + ">\t";
                    stringBuilder.Append(prefix);
                }

                stringBuilder.AppendLine(exception.Message);
                exception = exception.InnerException;
            }

            return stringBuilder.ToString();
        }
    }
}
