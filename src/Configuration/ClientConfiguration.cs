//Copyright(c) 2022 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-ldap-adapter/blob/main/LICENSE.md

using System.Collections.Generic;
using System.Linq;

namespace MultiFactor.Ldap.Adapter.Configuration
{
    public class ClientConfiguration
    {
        public ClientConfiguration()
        {
            BypassSecondFactorWhenApiUnreachable = true; //by default
            ServiceAccounts = new string[0];
            ServiceAccountsOrganizationUnit = new string[0];
            ActiveDirectoryGroup = new string[0];
            ActiveDirectory2FaGroup = new string[0];
            ActiveDirectory2FaBypassGroup = new string[0];

            LoadActiveDirectoryNestedGroups = true;

            UserNameTransformRules = new UserNameTransformationRules();
        }

        #region general settings

        /// <summary>
        /// Friendly client name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// LDAP server name or address
        /// </summary>
        public string LdapServer { get; set; }

        /// <summary>
        /// Bind user container
        /// </summary>
        public string LdapBaseDn { get; set; }

        /// <summary>
        /// Bypass second factor when MultiFactor API is unreachable
        /// </summary>
        public bool BypassSecondFactorWhenApiUnreachable { get; set; }

        /// <summary>
        /// Service accounts list - bind requests from its will be ignored
        /// </summary>
        public string[] ServiceAccounts { get; set; }

        /// <summary>
        /// Service accounts OU - bind requests with this OU will be ignored
        /// </summary>
        public string[] ServiceAccountsOrganizationUnit { get; set; }

        /// <summary>
        /// Only members of this groups allowed to access (Optional)
        /// </summary>
        public string[] ActiveDirectoryGroup { get; set; }

        /// <summary>
        /// Only members of this groups required to pass 2fa to access (Optional)
        /// </summary>
        public string[] ActiveDirectory2FaGroup { get; set; }
        /// <summary>
        /// Members of this group should not pass 2fa to access (Optional)
        /// </summary>
        public string[] ActiveDirectory2FaBypassGroup { get; set; }

        public bool LoadActiveDirectoryNestedGroups { get; set; }

        /// <summary>
        /// Username transfor rules
        /// </summary>
        public UserNameTransformationRules UserNameTransformRules { get; set; }
        #endregion

        #region API settings

        /// <summary>
        /// Multifactor API KEY
        /// </summary>
        public string MultifactorApiKey { get; set; }

        /// <summary>
        /// API Secret
        /// </summary>
        public string MultifactorApiSecret { get; set; }
        public AuthenticatedClientCacheConfig AuthenticationCacheLifetime { get; internal set; }

        #endregion

        public bool CheckUserGroups()
        {
            return
                ActiveDirectoryGroup.Any() || 
                ActiveDirectory2FaGroup.Any() || 
                ActiveDirectory2FaBypassGroup.Any();
        }
    }
}
