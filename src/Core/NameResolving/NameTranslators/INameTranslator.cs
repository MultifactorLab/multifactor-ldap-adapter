namespace MultiFactor.Ldap.Adapter.Core.NameResolving.NameTranslators
{
    public interface INameTranslator
    {
        public string Translate(string from, NameResolverContext context);
    }
}
