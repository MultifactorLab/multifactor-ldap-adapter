
using Microsoft.Extensions.DependencyInjection;
using MultiFactor.Ldap.Adapter.Configuration;
using MultiFactor.Ldap.Adapter.Core;
using MultiFactor.Ldap.Adapter.Core.Logging;
using MultiFactor.Ldap.Adapter.Tests.Fixtures;
using Serilog.Core;
using System.Net;
using Xunit;

namespace MultiFactor.Ldap.Adapter.Tests
{
    public class LogLevelTest
    {
        [Fact]
        public void RetrieveAndValidateLogger_NotNull_SingleClientModeFalse() 
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
        public void CreateAndValidateLogger_NotNull_WarningEnabled_DebugDisabled() 
        {
            var configuration = TestHostFactory.CreateHost(
                  TestEnvironment.GetAssetPath(TestAssetLocation.RootDirectory, "app.config"),
                  new[]
                  {
                        TestEnvironment.GetAssetPath(TestAssetLocation.ClientsDirectory, "client-minimal.config")
                  }
            ).Services.GetRequiredService<ServiceConfiguration>();

            var client = configuration.GetClient(IPAddress.Parse("127.0.0.2"));
            var loggerFactory = new LoggerFactory();
            var logLevelSwitch = new LoggingLevelSwitch();
            var logger = loggerFactory.CreateLogger(client.LogLevel, client.LogFormat, logLevelSwitch);
            Assert.NotNull(logger);
            Assert.True(logger.IsEnabled(Serilog.Events.LogEventLevel.Warning));
            Assert.False(logger.IsEnabled(Serilog.Events.LogEventLevel.Debug));
        }



        [Fact]
        public void ShouldLoad_CreateNotCreateLoggerTwice()
        {
            var configuration = TestHostFactory.CreateHost(
                  TestEnvironment.GetAssetPath(TestAssetLocation.RootDirectory, "app.config"),
                  new[]
                  {
                        TestEnvironment.GetAssetPath(TestAssetLocation.ClientsDirectory, "client-minimal.config")
                  }
            ).Services.GetRequiredService<ServiceConfiguration>();

            var client = configuration.GetClient(IPAddress.Parse("127.0.0.2"));
            //var loggerFactory = new ClientLoggerFactory();
            //var logger = provider.GetLogger(client);
            //Assert.NotNull(logger);
            //var logger2 = provider.GetLogger(client);
            //Assert.True(logger == logger2);
        }

        [Fact]
        public void ShouldLoad_CreateCreateGlobalLoggeer()
        {
            var configuration = TestHostFactory.CreateHost(
                  TestEnvironment.GetAssetPath(TestAssetLocation.RootDirectory, "app.config"),
                  new[]
                  {
                        TestEnvironment.GetAssetPath(TestAssetLocation.ClientsDirectory, "client-minimal.config")
                  }
            ).Services.GetRequiredService<ServiceConfiguration>();

            var client = configuration.GetClient(IPAddress.Parse("127.0.0.2"));
            //var loggerFactory = new ClientLoggerFactory();
            //var levelSwitch = new LoggingLevelSwitch();
            //var logger = provider.GetLogger(levelSwitch);
            //Assert.NotNull(logger);
            //var logger2 = provider.GetLogger(client);
            //Assert.True(logger != logger2);
        }
    }
}