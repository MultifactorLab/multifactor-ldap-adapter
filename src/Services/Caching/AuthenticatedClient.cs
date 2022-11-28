using System;

namespace MultiFactor.Ldap.Adapter.Services.Caching
{
    public class AuthenticatedClient
    {
        private readonly DateTime _authenticatedAt;

        public string Id { get; }
        public TimeSpan Elapsed => DateTime.Now - _authenticatedAt;

        public AuthenticatedClient(string id, DateTime authenticatedAt)
        {
            Id = id;
            _authenticatedAt = authenticatedAt;
        }

        public static AuthenticatedClient Create(params string[] components)
        {
            if (components is null) throw new ArgumentNullException(nameof(components));
            if (components.Length == 0) throw new ArgumentException(nameof(components));
            
            return new AuthenticatedClient(ParseId(components), DateTime.Now);
        }

        public static string ParseId(params string[] components) => string.Join('-', components);    
    }
}
