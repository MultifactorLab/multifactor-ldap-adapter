﻿using MultiFactor.Ldap.Adapter.Server.LdapStream;
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
    }
}
