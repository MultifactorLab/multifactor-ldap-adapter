using MultiFactor.Ldap.Adapter.Configuration;
using MultiFactor.Ldap.Adapter.Core.Abstractions;
using MultiFactor.Ldap.Adapter.Core.Requests;
using Serilog;
using System;

namespace MultiFactor.Ldap.Adapter.Server.LdapPacketModifiers
{
    public static class RequestModifierFactory
    {
        public static IRequestModifier<T> CreateModifier<T>(ClientConfiguration config, ILogger logger) 
            where T : LdapRequest
        {
            if (config is null) throw new ArgumentNullException(nameof(config));
            if (logger is null) throw new ArgumentNullException(nameof(logger));

            if (typeof(T) ==  typeof(BindRequest))
            {
                return (IRequestModifier<T>)new BindRequestModifier(config.LdapBaseDn, logger);
            }
            
            if (typeof(T) ==  typeof(SearchRequest))
            {
                return (IRequestModifier<T>)new SearchRequestModifier();
            }

            return (IRequestModifier<T>)new GenericBindRequestModifier();
        }
    }
}
 