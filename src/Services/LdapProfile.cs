//Copyright(c) 2022 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-ldap-adapter/blob/main/LICENSE.md

using System;
using System.Collections.Generic;
using System.Linq;

namespace MultiFactor.Ldap.Adapter.Services
{
    public class LdapProfile
    {
        public LdapProfile()
        {
            MemberOf = new List<string>();
        }

        public string Dn { get; set; }
        public string DisplayName { get; set; }
        public string Email { get; set; }
        public string Upn { get; set; }
        public List<string> MemberOf { get; set; }
        
        public string BaseDn
        {
            get
            {
                return GetBaseDn(Dn);
            }
        }

        public static string GetBaseDn(string dn)
        {
            var ncs = dn.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            var baseDn = ncs.Where(nc => nc.ToLower().StartsWith("dc="));
            return string.Join(",", baseDn);
        }
    }
}