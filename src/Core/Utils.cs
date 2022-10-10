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
using System.Collections;
using System.Text;
using System.Linq;
using System.IO;
using System.Threading.Tasks;

namespace MultiFactor.Ldap.Adapter.Core
{
    public static class Utils
    {
        public static Byte[] StringToByteArray(String hex, Boolean trimWhitespace = true)
        {
            if (trimWhitespace)
            {
                hex = hex.Replace(" ", "");
            }

            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }
            return bytes;
        }

        public static String ByteArrayToString(Byte[] bytes)
        {
            var hex = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes)
            {
                hex.Append($"{b:x2}");
            }
            return hex.ToString();
        }


        /// <summary>
        /// Used for debugging and testing...
        /// </summary>
        /// <param name="bits"></param>
        /// <returns></returns>
        public static String BitsToString(BitArray bits)
        {
            int i = 1;
            var derp = "";
            foreach (var bit in bits)
            {
                derp += Convert.ToInt32(bit);
                if (i % 8 == 0)
                {
                    derp += " ";
                }
                i++;
            }
            return derp.Trim();
        }


        /// <summary>
        /// Convert integer length to a byte array with BER encoding
        /// https://en.wikipedia.org/wiki/X.690#BER_encoding
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public static Byte[] IntToBerLength(Int32 length)
        {
            // Short notation
            if (length <= 127)
            {
                return new byte[] { (byte)length };
            }
            // Long notation
            else
            {
                var intbytes = BitConverter.GetBytes(length);
                Array.Reverse(intbytes);

                byte intbyteslength = (byte)intbytes.Length;


                var lengthByte = intbyteslength + 128;
                var berBytes = new byte[1 + intbyteslength];
                berBytes[0] = (byte)lengthByte;
                Buffer.BlockCopy(intbytes, 0, berBytes, 1, intbyteslength);
                return berBytes;
            }
        }


        /// <summary>
        /// Convert BER encoded length at offset to an integer
        /// </summary>
        /// <param name="bytes">Byte array</param>
        /// <param name="offset">Offset where the BER encoded length is located</param>
        /// <param name="berByteCount">Number of bytes used to represent BER encoded length</param>
        /// <returns></returns>
        public static async Task<BerLen> BerLengthToInt(Byte[] bytes, Int32 offset)
        {
            var stream = new MemoryStream(bytes, offset, bytes.Length - offset, false);
            return await BerLengthToInt(stream);
        }


        /// <summary>
        /// Get a BER length from a stream
        /// </summary>
        /// <param name="stream">Stream at position where BER length should be found</param>
        /// <param name="berByteCount">Number of bytes used to represent BER encoded length</param>
        /// <returns></returns>
        public static async Task<BerLen> BerLengthToInt(Stream stream)
        {
            var berByteCount = 1;   // The minimum length of a ber encoded length is 1 byte
            int attributeLength = 0;
            var berByte = new Byte[1];
            await stream.ReadAsync(berByte, 0, 1);
            if (berByte[0] >> 7 == 1)    // Long notation, first byte tells us how many bytes are used for the length
            {
                var lengthoflengthbytes = berByte[0] & 127;
                var lengthBytes = new Byte[lengthoflengthbytes];

                int bytesRead = 0;
                while (bytesRead < lengthoflengthbytes)
                {
                    var len = await stream.ReadAsync(lengthBytes, bytesRead, lengthoflengthbytes - bytesRead);
                    if (len == 0) break;
                    bytesRead += len;
                }

                Array.Reverse(lengthBytes);
                Array.Resize(ref lengthBytes, 4);   // this will of course explode if length is larger than a 32 bit integer
                attributeLength = BitConverter.ToInt32(lengthBytes, 0);
                berByteCount += lengthoflengthbytes;
            }
            else // Short notation, length contained in the first byte
            {
                attributeLength = berByte[0] & 127;
            }

            return new BerLen { Length = attributeLength, BerByteCount = berByteCount };
        }


        public static String Repeat(String stuff, Int32 n)
        {
            return String.Concat(Enumerable.Repeat(stuff, n));
        }
    }
}
