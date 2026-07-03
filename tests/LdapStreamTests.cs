using MultiFactor.Ldap.Adapter.Server.LdapStream;
using MultiFactor.Ldap.Adapter.Tests.Fixtures;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace MultiFactor.Ldap.Adapter.Tests
{
    public class LdapStreamTests
    {
        public static byte[] GetPacket(string packetName = "ldap-packet-dump.bin")
        {
            var path = TestEnvironment.GetAssetPath(TestAssetLocation.RootDirectory, packetName);
            byte[] fileData = File.ReadAllBytes(path);
            return fileData;
        }

        [Fact]
        public async Task LdapStream_ShouldInvalidatePackage()
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
        }

        [Fact]
        public async Task LdapStream_ShouldReadPackage()
        {
            var packet = GetPacket();
            using (var stream = new MemoryStream(packet))
            {
                var reader = new LdapStreamReader(stream);
                var result = await reader.ReadLdapPacket();
                Assert.True(result.PacketValid);
            }
        }

        [Fact]
        public async Task LdapStream_ShouldNotOverflow()
        {
            var packet = GetPacket();

            using (var stream = new MemoryStream(packet))
            {
                int bufferSize = 2048;

                var reader = new LdapStreamReader(stream, bufferSize);
                var result = await reader.ReadLdapPacket();
                Assert.True(result.PacketValid);
                Assert.True(result.Data.Length > 2048);
            }
        }

        [Fact]
        public async Task LdapStream_ShouldReadFragmentedPackage()
        {
            // a packet split between tcp segments: each read returns a single byte
            var packet = GetPacket();
            using (var stream = new ChunkedStream(packet, chunkSize: 1))
            {
                var reader = new LdapStreamReader(stream);
                var result = await reader.ReadLdapPacket();
                Assert.True(result.PacketValid);
                Assert.Equal(packet.Length, result.Data.Length);
            }
        }

        [Fact]
        public async Task LdapStream_EmptyStream_ShouldReturnEmptyPacket()
        {
            // closed connection: the proxy treats an empty packet as end of stream
            using (var stream = new MemoryStream())
            {
                var reader = new LdapStreamReader(stream);
                var result = await reader.ReadLdapPacket();
                Assert.False(result.PacketValid);
                Assert.Empty(result.Data);
            }
        }

        [Fact]
        public async Task LdapStream_ShouldReadSequentialFragmentedPackets()
        {
            // several packets on one connection, each read returns a single byte:
            // the reader must not lose the frame boundaries between packets
            var packet = GetPacket();
            var twoPackets = new byte[packet.Length * 2];
            packet.CopyTo(twoPackets, 0);
            packet.CopyTo(twoPackets, packet.Length);

            using (var stream = new ChunkedStream(twoPackets, chunkSize: 1))
            {
                var reader = new LdapStreamReader(stream);

                var first = await reader.ReadLdapPacket();
                Assert.True(first.PacketValid);
                Assert.Equal(packet.Length, first.Data.Length);

                var second = await reader.ReadLdapPacket();
                Assert.True(second.PacketValid);
                Assert.Equal(packet.Length, second.Data.Length);

                var end = await reader.ReadLdapPacket();
                Assert.False(end.PacketValid);
                Assert.Empty(end.Data);
            }
        }

        [Fact]
        public async Task LdapStream_ShouldNotReadVeryBigPacket()
        {
            var packet = GetPacket("ldap-packet-dump-big.bin");

            using (var stream = new MemoryStream(packet))
            {
                var reader = new LdapStreamReader(stream);
                var result = await reader.ReadLdapPacket();
                Assert.False(result.PacketValid);
                Assert.False(result.Data.Length > 2048);
            }
        }

        private class ChunkedStream : MemoryStream
        {
            private readonly int _chunkSize;

            public ChunkedStream(byte[] data, int chunkSize) : base(data)
            {
                _chunkSize = chunkSize;
            }

            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken)
            {
                return base.ReadAsync(buffer, offset, Math.Min(count, _chunkSize), cancellationToken);
            }
        }
    }
}
