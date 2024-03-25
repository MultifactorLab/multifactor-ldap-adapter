using Microsoft.Extensions.DependencyInjection;
using MultiFactor.Ldap.Adapter.Configuration;
using MultiFactor.Ldap.Adapter.Core.NameResolve;
using MultiFactor.Ldap.Adapter.Tests.Fixtures;
using System;
using System.Configuration;
using System.Net;
using Xunit;

namespace tests
{
    public class ConfigurationTests
    {
        [Fact]
        public void ReadConfiguration_ShouldReturnMultiConfig()
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
            var client = configuration.GetClient(IPAddress.Parse("127.0.0.2"));
            Assert.NotNull(client);
            Assert.Equal("test", client.MultifactorApiSecret);
        }

        [Fact]
        public void ReadConfiguration_ShouldReturnSingleConfig()
        {
            var configuration = TestHostFactory.CreateHost(
                TestEnvironment.GetAssetPath(TestAssetLocation.RootDirectory, "app.config"),
                new string[0]
            ).Services.GetRequiredService<ServiceConfiguration>();

            Assert.NotNull(configuration);
            Assert.True(configuration.SingleClientMode);
        }


        [Theory]
        [InlineData("root-empty-api-url.config", "Configuration error: 'multifactor-api-url' element not found")]
        [InlineData("root-empty-adapter-ldap-endpoint.config", "Configuration error: Neither 'adapter-ldap-endpoint' or 'adapter-ldaps-endpoint' configured")]
        [InlineData("root-empty-log-level.config", "Configuration error: 'logging-level' element not found")]
        public void ReadConfiguration_SingleModeAndEmptySettings_ShouldThrow(string asset, string errorMessage)
        {
            Func<ServiceConfiguration> configuration = () => TestHostFactory.CreateHost(
               TestEnvironment.GetAssetPath(TestAssetLocation.RootDirectory, asset),
               new string[0]
            ).Services.GetRequiredService<ServiceConfiguration>();
            var msg = Assert.Throws<Exception>(configuration).Message;
            Assert.Equal(errorMessage, msg);
        }

        [Fact]
        public void ReadConfiguration_MultiConfigWrongLdapServer_ShouldThrow()
        {
            Func<ServiceConfiguration> configuration = () => TestHostFactory.CreateHost(
                TestEnvironment.GetAssetPath(TestAssetLocation.RootDirectory, "app.config"),
                new[]
                {
                    TestEnvironment.GetAssetPath(TestAssetLocation.ClientsDirectory, "client-empty-ldap-server.config")
                }
            ).Services.GetRequiredService<ServiceConfiguration>();
            var message = Assert.Throws<Exception>(configuration).Message;
            Assert.Equal("Configuration error: 'ldap-server' element not found", message);
        }

        [Fact]
        public void ReadConfiguration_MultiConfigWrongClientIp_ShouldThrow()
        {
            Func<ServiceConfiguration> configuration = () => TestHostFactory.CreateHost(
                TestEnvironment.GetAssetPath(TestAssetLocation.RootDirectory, "app.config"),
                new[]
                {
                    TestEnvironment.GetAssetPath(TestAssetLocation.ClientsDirectory, "client-wrong-client-ip.config")
                }
            ).Services.GetRequiredService<ServiceConfiguration>();
            var message = Assert.Throws<FormatException>(configuration);
        }

        [Fact]
        public void ReadConfiguration_SingleConfigEmptyLdapServerIp_ShouldThrow()
        {
            Func<ServiceConfiguration> configuration = () => TestHostFactory.CreateHost(
                TestEnvironment.GetAssetPath(TestAssetLocation.RootDirectory, "root-empty-ldap-server.config"),
                new string[0]
            ).Services.GetRequiredService<ServiceConfiguration>();
            var message = Assert.Throws<ConfigurationErrorsException>(configuration);
        }

        [Fact]
        public void ReadConfiguration_MultiConfigEmptyLdapServerIp_ShouldThrow()
        {
            Func<ServiceConfiguration> configuration = () => TestHostFactory.CreateHost(
                TestEnvironment.GetAssetPath(TestAssetLocation.RootDirectory, "app.config"),
                new[]
                {
                    TestEnvironment.GetAssetPath(TestAssetLocation.ClientsDirectory, "root-empty-ldap-server.config")
                }
            ).Services.GetRequiredService<ServiceConfiguration>();
            var message = Assert.Throws<Exception>(configuration);
        }

        [Fact]
        public void ReadConfiguration_ShouldReadEnforcedLoginFormat_ShouldReturn()
        {
            var configuration = TestHostFactory.CreateHost(
                TestEnvironment.GetAssetPath(TestAssetLocation.RootDirectory, "app.config"),
                new[]
                {
                    TestEnvironment.GetAssetPath(TestAssetLocation.ClientsDirectory, "client-minimal-with-enforced-login-format.config"),
                    TestEnvironment.GetAssetPath(TestAssetLocation.ClientsDirectory, "client-minimal.config")
                }
            ).Services.GetRequiredService<ServiceConfiguration>();
            Assert.NotNull(configuration);
            Assert.False(configuration.SingleClientMode);
            var client = configuration.GetClient(IPAddress.Parse("127.0.0.3"));
            Assert.NotNull(client);
            Assert.Equal(NameType.Upn, client.EnforcedLoginFormat);
            var clientWithoutParam = configuration.GetClient(IPAddress.Parse("127.0.0.2"));
            Assert.NotNull(clientWithoutParam);
            Assert.Null(clientWithoutParam.EnforcedLoginFormat);
        }
    }
}
