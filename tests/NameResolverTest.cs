using Microsoft.Extensions.DependencyInjection;
using MultiFactor.Ldap.Adapter.Core.NameResolve;
using MultiFactor.Ldap.Adapter.Core.NameResolving;
using MultiFactor.Ldap.Adapter.Tests.Fixtures;
using Xunit;

namespace MultiFactor.Ldap.Adapter.Tests
{
    public class DomainResolverTest
    {
        [Theory]
        [InlineData("admin@domain", "admin@domain.test")]
        public void ShouldResolveName(string from, string to)
        {
            var host = TestHostFactory.CreateHost(
                TestEnvironment.GetAssetPath(TestAssetLocation.RootDirectory, "app.config"),
                new[]
                {
                    TestEnvironment.GetAssetPath(TestAssetLocation.ClientsDirectory, "client-minimal.config")
                }
            );
            var resolver = host.Services.GetRequiredService<NameResolverService>();
            var context = new NameResolverContext(new[] {
                new NetbiosDomainName
                {
                    Domain = "domain.test",
                    NetbiosName = "DOMAIN"
                }
            }, null);
            var result = resolver.Resolve(context, from, LdapIdentityFormat.Upn);
            Assert.Equal(result, to);
        }

        [Theory]
        [InlineData("admin@domain", LdapIdentityFormat.UidAndNetbios)]
        [InlineData("admin@domain.local", LdapIdentityFormat.Upn)]
        [InlineData("DOMAIN\\admin", LdapIdentityFormat.NetBIOSAndUid)]
        [InlineData("admin", LdapIdentityFormat.SamAccountName)]
        public void ShouldDetermineNameType(string name, LdapIdentityFormat expectedNameType)
        {
            var type = NameTypeDetector.GetType(name);
            Assert.Equal(expectedNameType, type);
        }

    }
}
