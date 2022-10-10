using System.Configuration;

namespace MultiFactor.Ldap.Adapter.Configuration
{
    public class UserNameTransformRulesSection : ConfigurationSection
    {
        [ConfigurationProperty("", IsDefaultCollection = true)]
        public UserNameTransformRulesCollection Members
        {
            get { return (UserNameTransformRulesCollection)base[""]; }
        }
    }
}
