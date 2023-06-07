using Microsoft.Extensions.DependencyInjection;
using MultiFactor.Ldap.Adapter.Configuration;
using MultiFactor.Ldap.Adapter.Server.LdapPacketModifiers;
using MultiFactor.Ldap.Adapter.Tests.Fixtures;
using System;
using System.Configuration;
using System.Net;
using Xunit;

namespace MultiFactor.Ldap.Adapter.Tests
{
    public class UserNameTransformRulesTests
    {

        [Fact]
        public void UsernameTransformRules_ShouldRead()
        {
            var configuration = TestHostFactory.CreateHost(
                TestEnvironment.GetAssetPath(TestAssetLocation.RootDirectory, "app.config"),
                new string[0]
            ).Services.GetRequiredService<ServiceConfiguration>();
            Assert.True(configuration.GetClient(IPAddress.Any).UserNameTransformRules.BeforeFirstFactor.Count > 0);
            Assert.True(configuration.GetClient(IPAddress.Any).UserNameTransformRules.BeforeSecondFactor.Count > 0);
        }


        [Fact]
        public void UsernameTransformRules_RootObsoleteRulesShouldBecomeSecondFactor()
        {
            var configuration = TestHostFactory.CreateHost(
                TestEnvironment.GetAssetPath(TestAssetLocation.RootDirectory, "username-transform-rules-obsolete-test.config"),
                new string[0]
            ).Services.GetRequiredService<ServiceConfiguration>();
            Assert.True(configuration.GetClient(IPAddress.Any).UserNameTransformRules.BeforeFirstFactor.Count == 0);
            Assert.True(configuration.GetClient(IPAddress.Any).UserNameTransformRules.BeforeSecondFactor.Count > 0);
        }


        [Fact]
        public void UsernameTransformRules_ShouldTransform()
        {
            var configuration = TestHostFactory.CreateHost(
                TestEnvironment.GetAssetPath(TestAssetLocation.RootDirectory, "username-transform-rules-obsolete-test.config"),
                new string[0]
            ).Services.GetRequiredService<ServiceConfiguration>();
            Assert.True(configuration.GetClient(IPAddress.Any).UserNameTransformRules.BeforeSecondFactor.Count > 0);
            var rules = configuration.GetClient(IPAddress.Any).UserNameTransformRules.BeforeSecondFactor;
            Assert.Equal("j.doves", UserNameTransformer.ProcessUserNameTransformRules("d.jones", rules));
        }

        [Fact]
        public void UsernameTransformRules_Empty_ShouldNotThorw()
        {
            var configuration = TestHostFactory.CreateHost(
                TestEnvironment.GetAssetPath(TestAssetLocation.RootDirectory, "username-transform-rules-empty-test.config"),
                new string[0]
            ).Services.GetRequiredService<ServiceConfiguration>();
            Assert.True(configuration.GetClient(IPAddress.Any).UserNameTransformRules.BeforeFirstFactor.Count == 0);
            Assert.True(configuration.GetClient(IPAddress.Any).UserNameTransformRules.BeforeSecondFactor.Count == 0);
        }

        [Fact]
        public void UsernameTransformRules_Wrong_ShouldThorw()
        {
            Func<ServiceConfiguration> configuration = () => TestHostFactory.CreateHost(
                TestEnvironment.GetAssetPath(TestAssetLocation.RootDirectory, "username-transform-rules-wrong-test.config"),
                new string[0]
            ).Services.GetRequiredService<ServiceConfiguration>();

            Assert.Throws<ConfigurationErrorsException>(configuration);
        }
    }
}
