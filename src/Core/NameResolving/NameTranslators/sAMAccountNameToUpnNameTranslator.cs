using System;
using System.Text.RegularExpressions;

namespace MultiFactor.Ldap.Adapter.Core.NameResolving.NameTranslators
{
    public class sAMAccountNameToUpnNameTranslator : INameTranslator
    {
        public string Translate(NameResolverContext nameTranslatorContext, string from)
        {
            if (nameTranslatorContext is null)
            {
                throw new ArgumentNullException(nameof(nameTranslatorContext));
            }

            if (string.IsNullOrEmpty(from))
            {
                throw new ArgumentException($"'{nameof(from)}' cannot be null or empty.", nameof(from));
            }

            if (nameTranslatorContext.Profile != null)
            {
                return nameTranslatorContext.Profile.Upn;
            }
            return from;
        }
    }
}
