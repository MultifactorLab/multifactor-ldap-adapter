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


        [ConfigurationProperty("BeforeFirstFactor")]
        public UserNameTransformRuleSetting BeforeFirstFactor
        {
            get
            {
                var url =
                (UserNameTransformRuleSetting)base["BeforeFirstFactor"];
                return url;
            }
        }

        [ConfigurationProperty("BeforeSecondFactor")]
        public UserNameTransformRuleSetting BeforeSecondFactor
        {
            get
            {
                var url =
                (UserNameTransformRuleSetting)base["BeforeSecondFactor"];
                return url;
            }
        }
    }
}
