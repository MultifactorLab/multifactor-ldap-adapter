namespace MultiFactor.Ldap.Adapter.Core.NameResolve
{
    public enum LdapIdentityFormat
    {
        None = 0,
        Upn = 1,
        UidAndNetbios = 2, // uid@netbios
        SamAccountName = 3,
        NetBIOSAndUid = 4, // NETBIOS\uid
        DistinguishedName = 5 
    }
}
