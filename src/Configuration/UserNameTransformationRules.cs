using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MultiFactor.Ldap.Adapter.Configuration
{
    public class UserNameTransformationRules
    {
        public List<UserNameTransformRulesElement> BeforeFirstFactor { get; set; } = new List<UserNameTransformRulesElement>();
        public List<UserNameTransformRulesElement> BeforeSecondFactor { get; set; } = new List<UserNameTransformRulesElement>();


        public void Load(UserNameTransformRulesSection section)
        {
            if(section == null)
            {
                return;
            }

            if (section.Members != null)
            {
                foreach (var member in section?.Members)
                {
                    if (member is UserNameTransformRulesElement rule)
                    {
                        BeforeSecondFactor.Add(rule);
                    }
                }
            }

            if (section.BeforeFirstFactor != null)
            {
                BeforeFirstFactor.AddRange(
                    section.BeforeFirstFactor.Members.Cast<UserNameTransformRulesElement>()
                );
            }

            if (section.BeforeSecondFactor != null)
            {
                BeforeSecondFactor.AddRange(
                    section.BeforeSecondFactor.Members.Cast<UserNameTransformRulesElement>()
                );
            }
        }
    }
}
