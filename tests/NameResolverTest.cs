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
            var context = new NameResolverContext();
            context.SetDomains(new[] { 
                new NetbiosDomainName {
                    Domain = "domain.test",
                    NetbiosName = "DOMAIN"
                }
            });
            var result = resolver.Resolve(context, from, NameType.Upn);
            Assert.Equal(result, to);
        }

        [Theory]
        [InlineData("admin@domain", NameType.UidAndNetbios)]
        [InlineData("admin@domain.local", NameType.Upn)]
        [InlineData("DOMAIN\\admin", NameType.NetBIOSAndUid)]
        [InlineData("admin", NameType.SamAccountName)]
        public void ShouldDetermineNameType(string name, NameType expectedNameType)
        {
            var type = NameTypeDetector.GetType(name);
            Assert.Equal(expectedNameType, type);
        }

    }
}
