
using Microsoft.Extensions.DependencyInjection;
using MultiFactor.Ldap.Adapter.Configuration;
using MultiFactor.Ldap.Adapter.Core.Logging;
using MultiFactor.Ldap.Adapter.Tests.Fixtures;
using Serilog;
using System;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace MultiFactor.Ldap.Adapter.Tests
{
    public class LogLevelTest
    {
        [Fact]
        public void ClientConfiguration_ShouldLoadLogLevel() 
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
        public void LoggerFactory_ShouldCreateLogger() 
        {
            var configuration = TestHostFactory.CreateHost(
                  TestEnvironment.GetAssetPath(TestAssetLocation.RootDirectory, "app.config"),
                  new[]
                  {
                        TestEnvironment.GetAssetPath(TestAssetLocation.ClientsDirectory, "client-minimal.config")
                  }
            ).Services.GetRequiredService<ServiceConfiguration>();

            var client = configuration.GetClient(IPAddress.Parse("127.0.0.2"));
            var logger = LoggerFactory.CreateLogger(client.LogLevel, client.LogFormat);
            Assert.NotNull(logger);
            Assert.True(logger.IsEnabled(Serilog.Events.LogEventLevel.Warning));
            Assert.False(logger.IsEnabled(Serilog.Events.LogEventLevel.Debug));
        }



        [Fact]
        public void LoggerProvider_ShouldNotCreateLoggerTwice()
        {
            var configuration = TestHostFactory.CreateHost(
                  TestEnvironment.GetAssetPath(TestAssetLocation.RootDirectory, "app.config"),
                  new[]
                  {
                        TestEnvironment.GetAssetPath(TestAssetLocation.ClientsDirectory, "client-minimal.config")
                  }
            ).Services.GetRequiredService<ServiceConfiguration>();

            var client = configuration.GetClient(IPAddress.Parse("127.0.0.2"));
            var provider = new ClientLoggerProvider();
            var logger = provider.GetLogger(client);
            Assert.NotNull(logger);
            var logger2 = provider.GetLogger(client);
            Assert.True(logger == logger2);
        }

        [Fact]
        public void LoggerProvider_ShouldBeConcurrent()
        {
            var configuration = TestHostFactory.CreateHost(
                  TestEnvironment.GetAssetPath(TestAssetLocation.RootDirectory, "app.config"),
                  new[]
                  {
                        TestEnvironment.GetAssetPath(TestAssetLocation.ClientsDirectory, "client-minimal.config")
                  }
            ).Services.GetRequiredService<ServiceConfiguration>();

            var client = configuration.GetClient(IPAddress.Parse("127.0.0.2"));
            var provider = new ClientLoggerProvider();
            ILogger logger1, logger2;
            Action createMethod = () => provider.GetLogger(client);
            int arraySize = 20;
            var bigArray = new Action[arraySize];
            Array.Fill(bigArray, createMethod);

            Parallel.Invoke(
                bigArray
            );
            logger1 = provider.GetLogger(client);
            logger2 = provider.GetLogger(client);
            Assert.True(logger1 == logger2);
             
        }
    }
}