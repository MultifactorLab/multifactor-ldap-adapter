//Copyright(c) 2021 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/MultiFactor.Ldap.Adapter/blob/main/LICENSE.md

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

namespace MultiFactor.Ldap.Adapter.Core
{
    public class Tag
    {
        /// <summary>
        /// Tag in byte form
        /// </summary>
        public Byte TagByte { get; internal set; }


        public Boolean IsConstructed
        {
            get
            {
                return (TagByte & (1 << 5)) != 0;                
            }
            set
            {
                var foo = new BitArray(new byte[] { TagByte });
                foo.Set(5, value);
                var temp = new byte[1];
                foo.CopyTo(temp, 0);
                TagByte = temp[0];
            }
        }

        public TagClass Class => (TagClass)(TagByte >> 6);
        public UniversalDataType? DataType => Class == TagClass.Universal ? (UniversalDataType?)(TagByte & 31) : null;
        public LdapOperation? LdapOperation => Class == TagClass.Application ? (LdapOperation?)(TagByte & 31) : null;
        public Byte? ContextType => Class == TagClass.Context ? (Byte?)(TagByte & 31) : null;


        /// <summary>
        /// Create an application tag
        /// </summary>
        /// <param name="operation"></param>
        /// <param name="isSequence"></param>
        public Tag(LdapOperation operation)
        {
            TagByte = (byte)((byte)operation + ((byte)TagClass.Application << 6));
        }


        /// <summary>
        /// Create a universal tag
        /// </summary>
        /// <param name="isSequence"></param>
        /// <param name="operation"></param>
        public Tag(UniversalDataType dataType)
        {
            TagByte = (byte)(dataType + ((byte)TagClass.Universal << 6));
        }


        /// <summary>
        /// Create a context tag
        /// </summary>
        /// <param name="isSequence"></param>
        /// <param name="operation"></param>
        public Tag(Byte context)
        {
            TagByte = (byte)(context + ((byte)TagClass.Context << 6));
        }


        /// <summary>
        /// Parses a raw tag byte
        /// </summary>
        /// <param name="tagByte"></param>
        /// <returns></returns>
        public static Tag Parse(Byte tagByte)
        {
            return new Tag { TagByte = tagByte };
        }


        private Tag()
        {
        }
    }
}
