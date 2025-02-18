using MultiFactor.Ldap.Adapter.Configuration;
using MultiFactor.Ldap.Adapter.Services;
using Xunit;

namespace MultiFactor.Ldap.Adapter.Tests;

public class PersonalDataTests
{
    [Fact]
    public void CreatePersonalData_NonePrivacyMode_ShouldFillEmail()
    {
        var email = "email";
        var descriptor = PrivacyModeDescriptor.Create(null);
        var profile = new LdapProfile();
        profile.Email = email;
        var personalData = new PersonalData(profile, descriptor);
        Assert.Equal(email, personalData.Email);
    }
    
    [Fact]
    public void CreatePersonalData_FullPrivacyMode_EmailShouldBeNull()
    {
        var email = "email";
        var descriptor = PrivacyModeDescriptor.Create("Full");
        var profile = new LdapProfile();
        profile.Email = email;
        var personalData = new PersonalData(profile, descriptor);
        Assert.Null(personalData.Email);
    }

    [Fact]
    public void CreatePersonalData_PartialPrivacyMode_ProvidedEmailNotNull()
    {
        var email = "email";
        var descriptor = PrivacyModeDescriptor.Create($"Partial:Email");
        var profile = new LdapProfile();
        profile.Email = email;
        var personalData = new PersonalData(profile, descriptor);
        Assert.Equal(email, personalData.Email);
    }
}