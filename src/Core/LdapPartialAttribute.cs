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
using System.Collections.Generic;
using System.Linq;

namespace MultiFactor.Ldap.Adapter.Core
{
    /// <summary>
    /// Convenience class for creating PartialAttributes
    /// </summary>
    public class LdapPartialAttribute : LdapAttribute
    {
        /// <summary>
        /// Partial attribute description
        /// </summary>
        public String Description => (String)ChildAttributes.FirstOrDefault().GetValue();


        /// <summary>
        /// Partial attribute values
        /// </summary>
        public List<String> Values => ChildAttributes[1].ChildAttributes.Select(o => (String)o.GetValue()).ToList();


        /// <summary>
        /// Create a partial Attribute from list of values
        /// </summary>
        /// <param name="attributeDescription"></param>
        /// <param name="attributeValues"></param>
        public LdapPartialAttribute(String attributeDescription, IEnumerable<String> attributeValues) : base(UniversalDataType.Sequence)
        {
            ChildAttributes.Add(new LdapAttribute(UniversalDataType.OctetString, attributeDescription));
            var values = new LdapAttribute(UniversalDataType.Set);
            values.ChildAttributes.AddRange(attributeValues.Select(o => new LdapAttribute(UniversalDataType.OctetString, o)));
            ChildAttributes.Add(values);
        }


        /// <summary>
        /// Create a partial attribute with a single value
        /// </summary>
        /// <param name="attributeDescription"></param>
        /// <param name="attributeValue"></param>
        public LdapPartialAttribute(String attributeDescription, String attributeValue) : this(attributeDescription, new List<String> { attributeValue })
        {

        }
    }
}
