using System;
using System.Text.RegularExpressions;

namespace MultiFactor.Ldap.Adapter.Core.NameResolving.NameTranslators
{
    public class NetbiosToUpnNameTranslator : INameTranslator
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

            foreach (var domain in nameTranslatorContext.Domains)
            {
                var regex = new Regex($"^{domain.NetbiosName.ToLower()}\\\\", RegexOptions.IgnoreCase);
                if (regex.IsMatch(from))
                {
                    var result = regex.Replace($"{from}@{domain.Domain.ToLower()}", "");
                    return result;
                }
            }
            return from;
        }
    }
}
