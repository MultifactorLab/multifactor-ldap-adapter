using System.Configuration;

namespace MultiFactor.Ldap.Adapter.Configuration
{
    public class UserNameTransformRulesElement : ConfigurationElement
    {
        [ConfigurationProperty("match", IsKey = false, IsRequired = true)]
        public string Match
        {
            get { return (string)this["match"]; }
        }

        [ConfigurationProperty("replace", IsKey = false, IsRequired = true)]
        public string Replace
        {
            get { return (string)this["replace"]; }
        }

        [ConfigurationProperty("count", IsKey = false, IsRequired = false)]
        public int? Count
        {
            get { return (int?)this["count"]; }
        }
    }
}
