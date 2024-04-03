namespace MultiFactor.Ldap.Adapter.Core.NameResolving.NameTranslators
{
    public interface INameTranslator
    {
        public string Translate(NameResolverContext context, string from);
    }
}
