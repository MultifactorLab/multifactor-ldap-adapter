//Copyright(c) 2021 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-ldap-adapter/blob/main/LICENSE.md

//MIT License
//Copyright(c) 2017 Verner Fortelius

//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in all
//copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//SOFTWARE.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MultiFactor.Ldap.Adapter.Core
{
    public class LdapPacket : LdapAttribute
    {
        public Int32 MessageId => ChildAttributes[0].GetValue<Int32>();


        /// <summary>
        /// Create a new Ldap packet with message id
        /// </summary>
        /// <param name="messageId"></param>
        public LdapPacket(Int32 messageId) : base(UniversalDataType.Sequence)
        {
            ChildAttributes.Add(new LdapAttribute(UniversalDataType.Integer, messageId));
        }


        /// <summary>
        /// Create a packet with tag
        /// </summary>
        /// <param name="tag"></param>
        private LdapPacket(Tag tag) : base(tag)
        {
        }


        /// <summary>
        /// Parse an ldap packet from a byte array. 
        /// Must be the complete packet
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static async Task<LdapPacket> ParsePacket(Byte[] bytes)
        {
            var packet = new LdapPacket(Tag.Parse(bytes[0]));
            var berLen = await Utils.BerLengthToInt(bytes, 1);
            packet.ChildAttributes.AddRange(await ParseAttributes(bytes, 1 + berLen.BerByteCount, berLen.Length));
            return packet;
        }


        /// <summary>
        /// Try parsing an ldap packet from a stream        
        /// </summary>      
        /// <param name="stream"></param>
        /// <param name="packet"></param>
        /// <returns>True if succesful. False if parsing fails or stream is empty</returns>
        public static async Task<LdapPacket> ParsePacket(Stream stream)
        {
            try
            {
                var tagByte = new byte[1];
                var i = await stream.ReadAsync(tagByte, 0, 1);
                if (i != 0)
                {
                    var contentLength = await Utils.BerLengthToInt(stream);
                    var contentBytes = new byte[contentLength.Length];
                    await stream.ReadAsync(contentBytes, 0, contentLength.Length);

                    var packet = new LdapPacket(Tag.Parse(tagByte[0]));
                    packet.ChildAttributes.AddRange(await ParseAttributes(contentBytes, 0, contentLength.Length));

                    if (packet.ChildAttributes.Any(attr => attr.LdapOperation == Core.LdapOperation.SearchResultDone))
                    {
                        return null; //thats all, stop reading
                    }

                    return packet;
                }
            }
            catch
            {
                throw;
                //Trace.TraceError($"Could not parse packet from stream {ex.Message}");                
            }

            return null;
        }
    }
}
