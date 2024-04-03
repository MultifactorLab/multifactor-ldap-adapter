using System.Text.RegularExpressions;

namespace MultiFactor.Ldap.Adapter.Core.NameResolving.NameTranslators
{
    public class DistinguishedNameToUpnTranslator : INameTranslator
    {
        public string Translate(NameResolverContext nameTranslatorContext, string from)
        {
            if (nameTranslatorContext.Profile != null)
            {
                return nameTranslatorContext.Profile.Upn;
            }
            return from;
        }
    }
}
