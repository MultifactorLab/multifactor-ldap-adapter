using MultiFactor.Ldap.Adapter.Configuration;
using MultiFactor.Ldap.Adapter.Core.Logging;
using System.Threading.Tasks;
using System;
using Xunit;
using Serilog;

namespace MultiFactor.Ldap.Adapter.Tests
{
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
            var client = GetClientConfiguration("Warning", null);
            var provider = new ClientLoggerProvider();
            int arraySize = 200;
            var bigArray = new ILogger[arraySize];
            Array.Fill(bigArray, null);

            Parallel.For(
                0, arraySize,
                new ParallelOptions()
                {
                    MaxDegreeOfParallelism = Math.Max(Environment.ProcessorCount - 1, 1),
                },
                (x) => bigArray[x] = provider.GetLogger(client)
            );

            for (int i = 1; i < arraySize; i++)
            {
                Assert.Same(bigArray[0], bigArray[i]);
            }

        }
    }
}
