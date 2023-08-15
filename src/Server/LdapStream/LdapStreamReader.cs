using MultiFactor.Ldap.Adapter.Core;
using MultiFactor.Ldap.Adapter.Server.LdapStream;
using System;
using System.IO;
using System.Threading.Tasks;

namespace MultiFactor.Ldap.Adapter.Server.LdapStream
{
    public class LdapStreamReader 
    {
        private byte[] _readBuffer = new byte[32192];
        private Stream _inputStream;
        public LdapStreamReader(Stream inputStream)
        {
            _inputStream = inputStream;
        }

        private LdapPacketBuffer GetResultPacket(int totalRead, bool packetValid)
        {
            var result = new LdapPacketBuffer()
            {
                Data = new byte[totalRead],
                PacketValid = packetValid
            };
            Array.Copy(_readBuffer, result.Data, totalRead);
            return result;
        }

        public async Task<LdapPacketBuffer> ReadLdapPacket()
        {
            int totalRead = await _inputStream.ReadAsync(_readBuffer, 0, 2);
            if (totalRead < 2)
            {
                return GetResultPacket(totalRead, false);
            }
            //  handle multi-octate BER LEN!!
            if (_readBuffer[1] >> 7 == 1)
            {
                totalRead += await _inputStream.ReadAsync(_readBuffer, totalRead, _readBuffer[1] & 127);
            }
            var berLen = await Utils.BerLengthToInt(_readBuffer, 1);
            int berLenWithHeading = berLen.Length + berLen.BerByteCount + 1;
            // read packet until end
            int attempts = 0;
            while (totalRead < berLenWithHeading)
            {
                var bytesReaded = await _inputStream.ReadAsync(_readBuffer, totalRead, berLen.Length);
                if (bytesReaded == 0)
                {
                    attempts++;
                    if (attempts > 3)
                    {
                        return GetResultPacket(totalRead, false);
                    }

                    await Task.Delay(500);
                    continue;
                }
                totalRead += bytesReaded;
                attempts = 0;
            }
            return GetResultPacket(totalRead, true);
        }
    }
}
