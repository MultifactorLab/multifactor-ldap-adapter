using MultiFactor.Ldap.Adapter.Server;
using MultiFactor.Ldap.Adapter.Tests.Fixtures;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace MultiFactor.Ldap.Adapter.Tests
{
    public class LdapStreamTests
    {
        public static byte[] GetPacket()
        {
            var path = TestEnvironment.GetAssetPath(TestAssetLocation.RootDirectory, "ldap-packet-dump.bin");
            byte[] fileData = File.ReadAllBytes(path);
            return fileData;
        }

        [Fact]
        public async Task LdapStream_ShouldValidatePackage()
        {
            var packet = GetPacket();
            int chunkSize = 0x10;
            byte[] chunk = new byte[chunkSize];
            Array.Copy(
                packet,
                packet.Length - chunkSize,
                chunk,
                0,
                chunkSize);

            using (var stream = new MemoryStream(chunk))
            {
                var reader = new LdapStreamReader(stream);
                var result = await reader.ReadLdapPacket();
                Assert.False(result.PacketValid);
            }
            using (var stream = new MemoryStream(packet))
            {
                var reader = new LdapStreamReader(stream);
                var result = await reader.ReadLdapPacket();
                Assert.True(result.PacketValid);
            }
        }
    }
}
