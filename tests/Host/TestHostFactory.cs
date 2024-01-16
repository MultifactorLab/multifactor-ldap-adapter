
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using MultiFactor.Ldap.Adapter.Configuration;
using MultiFactor.Ldap.Adapter.Configuration.Core;
using MultiFactor.Ldap.Adapter.Core.NameResolve;
using MultiFactor.Ldap.Adapter.Tests.Fixtures.ConfigLoading;
using Serilog;
using System;

namespace MultiFactor.Ldap.Adapter.Tests.Fixtures
{
    internal class TestHostFactory
    {
        private static IHost BuildHost(Action<IServiceCollection> configureServices = null)
        {
            var builder = Host.CreateDefaultBuilder();
            builder.ConfigureServices(configureServices);
            return builder.Build();
        }

        public static IHost CreateHost(string rootConfigPath, string[] clientConfigPaths)
        {
            return BuildHost(services =>
            {
                Log.Logger = Mock.Of<ILogger>();
                var testConfigProviderOptions = new TestConfigProviderOptions()
                {
                    RootConfigFilePath = rootConfigPath,
                    ClientConfigFilePaths = clientConfigPaths
                };
                var configProvider = new TestConfigProvider(testConfigProviderOptions);
                services.AddSingleton<IConfigurationProvider>(configProvider);
                services.AddSingleton(Log.Logger);
                services.AddSingleton<ServiceConfiguration>();
                services.AddTransient<NameResolverService>();
            });
        }
    }
}
