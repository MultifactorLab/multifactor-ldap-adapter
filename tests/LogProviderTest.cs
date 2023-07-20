using MultiFactor.Ldap.Adapter.Configuration;
using MultiFactor.Ldap.Adapter.Core.Logging;
using System.Threading.Tasks;
using System;
using Xunit;

namespace MultiFactor.Ldap.Adapter.Tests
{
    internal class TestableClientLoggerProvider : ClientLoggerProvider
    {
        public int Count => _loggerMap.Count;
    }
    public class LogProviderTest
    {
        public static ClientConfiguration GetClientConfiguration(string logLevel = "Debug", string logFormat = "json")
        {
            return new ClientConfiguration()
            {   
                Name = "Client",
                LogLevel = logLevel,
                LogFormat = logFormat
            };
        }

        [Fact]
        public void LoggerFactory_ShouldCreateLogger()
        {
            var client = GetClientConfiguration("Warning", null);
            var logger = LoggerFactory.CreateLogger(client.LogLevel, client.LogFormat);
            Assert.NotNull(logger);
            Assert.True(logger.IsEnabled(Serilog.Events.LogEventLevel.Warning));
            Assert.False(logger.IsEnabled(Serilog.Events.LogEventLevel.Debug));
        }


        [Fact]
        public void LoggerProvider_ShouldNotCreateLoggerTwice()
        {
            var client = GetClientConfiguration("Warning", null);

            var provider = new ClientLoggerProvider();
            var logger = provider.GetLogger(client);
            Assert.NotNull(logger);
            var logger2 = provider.GetLogger(client);
            Assert.True(logger == logger2);
        }

        [Fact]
        public void LoggerProvider_ShouldBeConcurrent()
        {
            var provider = new TestableClientLoggerProvider();
            int interationMax = 20;
 
            Parallel.For(
                0, interationMax,
                new ParallelOptions()
                {
                    MaxDegreeOfParallelism = Math.Max(Environment.ProcessorCount - 1, 1),
                },
                (x) => provider.GetLogger(GetClientConfiguration("Warning", null))
            );
            Assert.True(provider.Count == 1);
        }
    }
}
    