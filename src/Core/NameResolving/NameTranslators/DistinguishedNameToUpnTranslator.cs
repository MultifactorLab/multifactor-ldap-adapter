using System;

namespace MultiFactor.Ldap.Adapter.Core.NameResolving.NameTranslators
{
    public class DistinguishedNameToUpnTranslator : INameTranslator
    {
        public string Translate(NameResolverContext nameTranslatorContext, string from)
        {
            if (nameTranslatorContext is null)
            {
                throw new ArgumentNullException(nameof(nameTranslatorContext));
            }

            if (from is null)
            {
                throw new ArgumentNullException(nameof(from));
            }

            if (nameTranslatorContext.Profile != null)
            {
                return nameTranslatorContext.Profile.Upn;
            }
            return from;
        }
    }
}
