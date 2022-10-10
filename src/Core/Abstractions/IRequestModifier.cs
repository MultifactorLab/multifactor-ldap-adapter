using MultiFactor.Ldap.Adapter.Core.Requests;

namespace MultiFactor.Ldap.Adapter.Core.Abstractions
{
    /// <summary>
    /// Provides methods for LDAP request modification.
    /// </summary>
    /// <typeparam name="T">Concrete type of request.</typeparam>
    public interface IRequestModifier<T> where T : LdapRequest
    {
        /// <summary>
        /// Modifies request content and returns new instance of modified request.
        /// </summary>
        /// <param name="request">Original request.</param>
        /// <returns>Modified request.</returns>
        T Modify(T request);
    }
}
