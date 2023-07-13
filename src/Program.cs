using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MultiFactor.Ldap.Adapter.Configuration;
using MultiFactor.Ldap.Adapter.Configuration.Core;
using MultiFactor.Ldap.Adapter.Server;
using MultiFactor.Ldap.Adapter.Services;
using MultiFactor.Ldap.Adapter.Services.Caching;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Compact;
using System;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
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

                if (host != null)
                {
                    host.StopAsync();
                }
            }

        }

        private static void ConfigureServices(IServiceCollection services)
        {
            //create logging
            var levelSwitch = ConfigureLogging(services);

            services.AddSingleton<IConfigurationProvider, ConfigurationProvider>();
            services.AddSingleton(sp => {
                var configurationProvider = sp.GetRequiredService<IConfigurationProvider>();
                var logger = sp.GetRequiredService<ILogger>();
                var serviceConf = new ServiceConfiguration(configurationProvider, logger);
                SetLogLevel(serviceConf.LogLevel, levelSwitch);
                if (serviceConf.ServerConfig.AdapterLdapsEndpoint != null)
                {
                    GetOrCreateTlsCertificate(Core.Constants.ApplicationPath, serviceConf, Log.Logger);
                }
                return serviceConf;
            });

            services.AddSingleton(prov => new RandomWaiter(prov.GetRequiredService<ServiceConfiguration>().InvalidCredentialDelay));
            services.AddSingleton<MultiFactorApiClient>();
            services.AddSingleton<LdapProxyFactory>();
            services.AddSingleton<LdapServersFactory>();
            services.AddSingleton<AuthenticatedClientCache>();
            services.AddMemoryCache();
            services.AddSingleton(prov => prov.GetRequiredService<LdapServersFactory>().CreateServers());
            services.AddHostedService<ServerHost>();
        }

        private static LoggingLevelSwitch ConfigureLogging(IServiceCollection services)
        {
            var levelSwitch = new LoggingLevelSwitch(LogEventLevel.Information);
            var loggerConfiguration = new LoggerConfiguration()
                .MinimumLevel.ControlledBy(levelSwitch);

            var formatter = GetLogFormatter();
            if (formatter != null)
            {
                loggerConfiguration
                    .WriteTo.Console(formatter)
                    .WriteTo.File(formatter, $"{Core.Constants.ApplicationPath}logs{Path.DirectorySeparatorChar}log-.txt", rollingInterval: RollingInterval.Day);
            }
            else
            {
                loggerConfiguration
                    .WriteTo.Console()
                    .WriteTo.File($"{Core.Constants.ApplicationPath}logs{Path.DirectorySeparatorChar}log-.txt", rollingInterval: RollingInterval.Day);
            }
            Log.Logger = loggerConfiguration.CreateLogger();
            services.AddSingleton(Log.Logger);
            return levelSwitch;
        }

        private static void SetLogLevel(string level, LoggingLevelSwitch levelSwitch)
        {
            switch (level)
            {
                case "Debug":
                    levelSwitch.MinimumLevel = LogEventLevel.Debug;
                    break;
                case "Info":
                    levelSwitch.MinimumLevel = LogEventLevel.Information;
                    break;
                case "Warn":
                    levelSwitch.MinimumLevel = LogEventLevel.Warning;
                    break;
                case "Error":
                    levelSwitch.MinimumLevel = LogEventLevel.Error;
                    break;
            }

            Log.Logger.Information($"Logging level: {levelSwitch.MinimumLevel}");
        }

        private static void GetOrCreateTlsCertificate(string path, ServiceConfiguration configuration, Serilog.ILogger logger)
        {
            var certDirectory = $"{path}tls";
            if (!Directory.Exists(certDirectory))
            {
                Directory.CreateDirectory(certDirectory);
            }

            var certPath = $"{certDirectory}{Path.DirectorySeparatorChar}certificate.pfx";
            if (!File.Exists(certPath))
            {
                var subj = Dns.GetHostEntry("").HostName;

                logger.Debug($"Generating self-signing certificate for TLS with subject CN={subj}");

                var certService = new CertificateService();
                var cert = certService.GenerateCertificate(subj);

                var data = cert.Export(X509ContentType.Pfx);
                File.WriteAllBytes(certPath, data);

                logger.Information($"Self-signed certificate with subject CN={subj} saved to {certPath}");

                configuration.X509Certificate = cert;
            }
            else
            {
                logger.Debug($"Loading certificate for TLS from {certPath}");
                configuration.X509Certificate = new X509Certificate2(certPath);
            }
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

        private static ITextFormatter GetLogFormatter()
        {
            var format = ServiceConfiguration.GetLogFormat();
            switch (format?.ToLower())
            {
                case "json":
                    return new RenderedCompactJsonFormatter();
                default:
                    return null;
            }
        }
    }
}
