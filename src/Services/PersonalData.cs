using MultiFactor.Ldap.Adapter.Configuration;

namespace MultiFactor.Ldap.Adapter.Services;

public class PersonalData
{
    public string Email { get; private set; }

    public PersonalData(LdapProfile userProfile, PrivacyModeDescriptor privacyModeDescriptor)
    {
        Email = userProfile.Email;
        
        switch (privacyModeDescriptor.Mode)
        {
            case PrivacyMode.Full:
                Email = null;
                break;
            
            case PrivacyMode.Partial:

                if (!privacyModeDescriptor.HasField("Email"))
                {
                    Email = null;
                }
                break;
        }
    }
}