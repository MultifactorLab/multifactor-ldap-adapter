
using Microsoft.Extensions.DependencyInjection;
using MultiFactor.Ldap.Adapter.Configuration;
using MultiFactor.Ldap.Adapter.Core;
using MultiFactor.Ldap.Adapter.Tests.Fixtures;
using System.Net;
using Xunit;

namespace MultiFactor.Ldap.Adapter.Tests
{
    public class LogLevelTest
    {
        [Fact]
        public void ShouldLoad_ClientLogLevel() 
        {
            var configuration = TestHostFactory.CreateHost(
              TestEnvironment.GetAssetPath(TestAssetLocation.RootDirectory, "app.config"),
              new[]
              {
                    TestEnvironment.GetAssetPath(TestAssetLocation.ClientsDirectory, "client-minimal.config")
              }
            ).Services.GetRequiredService<ServiceConfiguration>();

            Assert.NotNull(configuration);
            Assert.False(configuration.SingleClientMode);

            Assert.Equal("Debug", configuration.LogLevel);
            Assert.Null(configuration.LogFormat);

            var client = configuration.GetClient(IPAddress.Parse("127.0.0.2"));
            Assert.Equal("Warn", client.LogLevel);
            Assert.Equal("json", client.LogFormat);
        }

        [Fact]
        public void ShouldLoad_CreateLoggingContext() 
        {
            var configuration = TestHostFactory.CreateHost(
                  TestEnvironment.GetAssetPath(TestAssetLocation.RootDirectory, "app.config"),
                  new[]
                  {
                        TestEnvironment.GetAssetPath(TestAssetLocation.ClientsDirectory, "client-minimal.config")
                  }
            ).Services.GetRequiredService<ServiceConfiguration>();

            var client = configuration.GetClient(IPAddress.Parse("127.0.0.2"));
            var provider = new ClientLoggerFactory(null);
            var logger = provider.GetLogger(client);
            Assert.NotNull(logger);
            Assert.True(logger.IsEnabled(Serilog.Events.LogEventLevel.Warning));
            Assert.False(logger.IsEnabled(Serilog.Events.LogEventLevel.Debug));
        }
    }



}
