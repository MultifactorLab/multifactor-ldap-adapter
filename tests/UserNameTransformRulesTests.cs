using Microsoft.Extensions.DependencyInjection;
using MultiFactor.Ldap.Adapter.Configuration;
using MultiFactor.Ldap.Adapter.Server.LdapPacketModifiers;
using MultiFactor.Ldap.Adapter.Tests.Fixtures;
using System;
using System.Configuration;
using System.Linq;
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
        public void UsernameTransformRules_RootObsoleteRulesShouldBeAddedToSecondFactor()
        {
            var configuration = TestHostFactory.CreateHost(
                TestEnvironment.GetAssetPath(TestAssetLocation.RootDirectory, "username-transform-rules-obsolete-with-new-test.config"),
                new string[0]
            ).Services.GetRequiredService<ServiceConfiguration>();
            var client = configuration.GetClient(IPAddress.Any);
            Assert.True(client.UserNameTransformRules.BeforeFirstFactor.Count == 1);
            Assert.True(client.UserNameTransformRules.BeforeSecondFactor.Count == 2);
            var toMatch = client.UserNameTransformRules.BeforeSecondFactor.Select(x => x.Match);
            Assert.Contains(toMatch, x => x == "d.jones");
            Assert.Contains(toMatch, x => x == "d.jones2");
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
