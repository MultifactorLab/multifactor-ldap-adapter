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
using System.Linq;
using System.Threading.Tasks;

namespace MultiFactor.Ldap.Adapter.Core.Requests
{
    /// <summary>
    /// LDAP request wrapper.
    /// </summary>
    public abstract class LdapRequest
    {
        /// <summary>
        /// Request packet.
        /// </summary>
        public LdapPacket Packet { get; }

        /// <summary>
        /// Request type.
        /// </summary>
        public LdapRequestType RequestType { get; }

        protected LdapRequest(LdapPacket packet, LdapRequestType requestType)
        {
            Packet = packet;
            RequestType = requestType;
        }

        public static LdapRequest Create(LdapPacket packet)
        {
            if (packet is null) throw new ArgumentNullException(nameof(packet));

            var bindReq = packet.ChildAttributes.SingleOrDefault(x => x.LdapOperation == LdapOperation.BindRequest);
            if (bindReq != null)
            {
                return new BindRequest(packet, bindReq);
            }

            var searchReq = packet.ChildAttributes.SingleOrDefault(x => x.LdapOperation == LdapOperation.SearchRequest);
            if (searchReq != null)
            {
                return new SearchRequest(packet, searchReq);
            }

            return new GenericRequest(packet);
        }

        public static async Task<LdapRequest> FromBytesAsync(byte[] bytes)
        {
            if (bytes is null) throw new ArgumentNullException(nameof(bytes));

            var packet = await LdapPacket.ParsePacket(bytes);
            return Create(packet);
        }

        /// <summary>
        /// Returns concrete type of request.
        /// Throws exception if specified type does not match with the real request type.
        /// </summary>
        /// <typeparam name="T">Concrete request type.</typeparam>
        /// <returns>Concrete type of request.</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public T As<T>() where T : LdapRequest
        {
            if (this is T) return (T)this;
            throw new InvalidOperationException("Incorrect LDAP request type");
        }
    }
}
