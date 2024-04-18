
using System;

namespace MultiFactor.Ldap.Adapter.Core.NameResolving.NameTranslators
{
    public class UpnFromProfileNameTranslator : INameTranslator
    {
        public string Translate(NameResolverContext nameResolverContext, string from)
        {
            if (nameResolverContext is null)
            {
                throw new ArgumentNullException(nameof(nameResolverContext));
            }

            if (string.IsNullOrEmpty(from))
            {
                throw new ArgumentException($"'{nameof(from)}' cannot be null or empty.", nameof(from));
            }

            if (nameResolverContext.Profile != null)
            {
                return nameResolverContext.Profile.Upn;
            }
            return from;
        }
    }
}
