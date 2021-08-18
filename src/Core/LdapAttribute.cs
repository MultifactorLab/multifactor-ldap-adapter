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
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MultiFactor.Ldap.Adapter.Core
{
    public class LdapAttribute
    {
        private Tag _tag;
        public Byte[] Value = new Byte[0];
        public List<LdapAttribute> ChildAttributes = new List<LdapAttribute>();

        public TagClass Class => _tag.Class;
        public Boolean IsConstructed => _tag.IsConstructed || ChildAttributes.Any();
        public LdapOperation? LdapOperation => _tag.LdapOperation;
        public UniversalDataType? DataType => _tag.DataType;
        public Byte? ContextType => _tag.ContextType;


        /// <summary>
        /// Create an application attribute
        /// </summary>
        /// <param name="operation"></param>
        /// <param name="isConstructed"></param>
        public LdapAttribute(LdapOperation operation)
        {
            _tag = new Tag(operation);
        }


        /// <summary>
        /// Create an application attribute
        /// </summary>
        /// <param name="operation"></param>
        /// <param name="isConstructed"></param>
        /// <param name="value"></param>
        public LdapAttribute(LdapOperation operation, Object value)
        {
            _tag = new Tag(operation);
            Value = GetBytes(value);
        }


        /// <summary>
        /// Create a universal attribute
        /// </summary>
        /// <param name="dataType"></param>
        /// <param name="isConstructed"></param>
        public LdapAttribute(UniversalDataType dataType)
        {
            _tag = new Tag(dataType);
        }


        /// <summary>
        /// Create a universal attribute
        /// </summary>
        /// <param name="dataType"></param>
        /// <param name="isConstructed"></param>
        /// <param name="value"></param>
        public LdapAttribute(UniversalDataType dataType, Object value)
        {
            _tag = new Tag(dataType);
            Value = GetBytes(value);
        }


        /// <summary>
        /// Create a context attribute
        /// </summary>
        /// <param name="contextType"></param>
        /// <param name="isConstructed"></param>
        public LdapAttribute(Byte contextType)
        {
            _tag = new Tag(contextType);
        }


        /// <summary>
        /// Create a context attribute
        /// </summary>
        /// <param name="contextType"></param>
        /// <param name="isConstructed"></param>
        /// <param name="value"></param>
        public LdapAttribute(Byte contextType, Object value)
        {
            _tag = new Tag(contextType);
            Value = GetBytes(value);
        }


        /// <summary>
        /// Create an attribute with tag
        /// </summary>
        /// <param name="tag"></param>
        protected LdapAttribute(Tag tag)
        {
            _tag = tag;
        }


        /// <summary>
        /// Get the byte representation of the attribute and its children
        /// </summary>
        /// <returns></returns>
        public Byte[] GetBytes()
        {
            var attributeBytes = new List<Byte>();
            BuildAttribute(attributeBytes);
            return attributeBytes.ToArray();
        }


        /// <summary>
        /// Recursively build the attribute
        /// </summary>
        /// <param name="attributeBytes"></param>
        private void BuildAttribute(List<Byte> attributeBytes)
        {
            var contentBytes = new List<Byte>();
            if (ChildAttributes.Any())
            {
                _tag.IsConstructed = true;
                ChildAttributes.ForEach(o => contentBytes.AddRange(o.GetBytes()));
            }
            else
            {
                contentBytes.AddRange(Value);
            }

            attributeBytes.Add(_tag.TagByte);
            attributeBytes.AddRange(Utils.IntToBerLength(contentBytes.Count));
            attributeBytes.AddRange(contentBytes);
        }


        /// <summary>
        /// Get a typed value
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetValue<T>()
        {
            return (T)Convert.ChangeType(GetValue(), typeof(T));
        }


        /// <summary>
        /// Get an object value
        /// </summary>
        /// <returns></returns>
        public object GetValue()
        {
            if (_tag.Class == TagClass.Universal)
            {
                switch (_tag.DataType)
                {
                    case UniversalDataType.Boolean:
                        return BitConverter.ToBoolean(Value, 0);

                    case UniversalDataType.Integer:
                        var intbytes = new Byte[4];
                        Buffer.BlockCopy(Value, 0, intbytes, 4 - Value.Length, Value.Length);
                        Array.Reverse(intbytes);
                        return BitConverter.ToInt32(intbytes, 0);
                    case UniversalDataType.Enumerated:
                        return (LdapResult)Value[0];
                    default:
                        return Encoding.UTF8.GetString(Value, 0, Value.Length);
                }
            }

            // todo add rest if needed
            return Encoding.UTF8.GetString(Value, 0, Value.Length);
        }


        /// <summary>
        /// Convert the value to its byte form
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private Byte[] GetBytes(Object value)
        {
            switch (value)
            {
                case String _value:
                    return Encoding.UTF8.GetBytes(_value);

                case Int32 _value:
                    return BitConverter.GetBytes(_value).Reverse().ToArray();

                case Boolean _value:
                    return BitConverter.GetBytes(_value);

                case Byte _value:
                    return new Byte[] { _value };

                case Byte[] _value:
                    return _value;

                default:
                    throw new InvalidOperationException($"Nothing found for {value.GetType()}");
            }
        }


        /// <summary>
        /// Parse the child attributes
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="currentPosition"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        protected static List<LdapAttribute> ParseAttributes(Byte[] bytes, Int32 currentPosition, Int32 length)
        {
            var list = new List<LdapAttribute>();
            while (currentPosition < length)
            {
                var tag = Tag.Parse(bytes[currentPosition]);
                currentPosition++;
                var attributeLength = Utils.BerLengthToInt(bytes, currentPosition, out int i);
                currentPosition += i;

                var attribute = new LdapAttribute(tag);
                if (tag.IsConstructed && attributeLength > 0)
                {
                    try
                    {
                        attribute.ChildAttributes = ParseAttributes(bytes, currentPosition, currentPosition + attributeLength);
                    }
                    catch { }
                }
                else
                {
                    attribute.Value = new Byte[attributeLength];
                    Buffer.BlockCopy(bytes, currentPosition, attribute.Value, 0, attributeLength);
                }
                list.Add(attribute);

                currentPosition += attributeLength;
            }
            return list;
        }
    }
}
