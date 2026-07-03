using MultiFactor.Ldap.Adapter.Core;
using Serilog;
using System;
using System.IO;
using System.Threading.Tasks;

namespace MultiFactor.Ldap.Adapter.Server.LdapStream
{
    public class LdapStreamReader 
    {
        private const int DEFAULT_BUFFER_SIZE = 32768;
        private const int MAX_PACKET_SIZE = 64 * Constants.BYTES_IN_MB;

        private byte[] _readBuffer;
        private Stream _inputStream;
        private readonly ILogger _logger;

        public LdapStreamReader(Stream inputStream, ILogger logger = null) : this(inputStream, DEFAULT_BUFFER_SIZE, logger)
        { }
        public LdapStreamReader(Stream inputStream, int bufferSize, ILogger logger = null)
        {
            _readBuffer = new byte[bufferSize];
            _inputStream = inputStream;
            _logger = logger ?? Log.Logger;
        }

        private LdapPacketBuffer GetResultPacket(byte[] buffer, int totalRead, bool packetValid)
        {
            var result = new LdapPacketBuffer()
            {
                Data = new byte[totalRead],
                PacketValid = packetValid
            };
            Array.Copy(buffer, result.Data, totalRead);
            return result;
        }

        public async Task<LdapPacketBuffer> ReadLdapPacket()
        {
            int totalRead = await _inputStream.ReadAsync(_readBuffer, 0, 2);
            if (totalRead < 2)
            {
                return GetResultPacket(_readBuffer, totalRead, false);
            }
            //  handle multi-octet BER LEN
            if (_readBuffer[1] >> 7 == 1)
            {
                totalRead += await _inputStream.ReadAsync(_readBuffer, totalRead, _readBuffer[1] & 127);
            }
            var berLen = await Utils.BerLengthToInt(_readBuffer, 1);
            int berLenWithHeading = berLen.Length + berLen.BerByteCount + 1;
            if (berLen.Length < 0 || berLenWithHeading > MAX_PACKET_SIZE)
            {
                _logger.Warning("LDAP packet of {size} byte(s) exceeds the {limit} byte(s) limit and will be bypassed without processing", berLenWithHeading, MAX_PACKET_SIZE);
                return GetResultPacket(_readBuffer, totalRead, false);
            }
            if(berLenWithHeading >= _readBuffer.Length)
            {
                var newBuffer = new byte[berLenWithHeading];
                Array.Copy(_readBuffer, newBuffer, totalRead);
                _readBuffer = newBuffer;
            }
            // read packet until end
            int attempts = 0;
            while (totalRead < berLenWithHeading)
            {
                var bytesReaded = await _inputStream.ReadAsync(_readBuffer, totalRead, berLenWithHeading - totalRead);
                if (bytesReaded == 0)
                {
                    attempts++;
                    if (attempts > 3)
                    {
                        return GetResultPacket(_readBuffer, totalRead, false);
                    }

                    await Task.Delay(500);
                    continue;
                }
                totalRead += bytesReaded;
                attempts = 0;
            }
            return GetResultPacket(_readBuffer, totalRead, true);
        }
    }
}
