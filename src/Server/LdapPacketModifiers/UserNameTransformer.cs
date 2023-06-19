using Serilog;
using MultiFactor.Ldap.Adapter.Configuration;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace MultiFactor.Ldap.Adapter.Server.LdapPacketModifiers
{
    public static class UserNameTransformer
    {
        public static string ProcessUserNameTransformRules(string userName, List<UserNameTransformRulesElement> rules, ILogger logger = null)
        {
            foreach (var rule in rules)
            {
                var regex = new Regex(rule.Match);
                var before = userName;
                if (rule.Count != null)
                {
                    userName = regex.Replace(userName, rule.Replace, rule.Count.Value);
                }
                else
                {
                    userName = regex.Replace(userName, rule.Replace);
                }

                if (logger != null && before != userName)
                {
                    logger.Debug($"Transformed username {before} => {userName}");
                }
            }

            return userName;
        }
    }
}
