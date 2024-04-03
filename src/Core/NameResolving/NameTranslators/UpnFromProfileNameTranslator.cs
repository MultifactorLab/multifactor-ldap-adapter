
namespace MultiFactor.Ldap.Adapter.Core.NameResolving.NameTranslators
{
    public class UpnFromProfileNameTranslator : INameTranslator
    {
        public string Translate(NameResolverContext nameResolverContext, string from)
        {
            if (nameResolverContext.Profile != null)
            {
                return nameResolverContext.Profile.Upn;
            }
            return from;
        }
    }
}
