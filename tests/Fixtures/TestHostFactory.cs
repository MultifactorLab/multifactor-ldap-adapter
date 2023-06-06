
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

namespace MultiFactor.Ldap.Adapter.Tests.Fixtures
{
    internal class TestHostFactory
    {
        public static IHost CreateHost(Action<IServiceCollection>? configureServices = null)
        {
            var builder = Host.CreateDefaultBuilder();
            builder.ConfigureServices(configureServices);
            return builder.Build();
        }
    }
}
