namespace MultiFactor.Ldap.Adapter.Core.NameResolve
{
    public enum NameType
    {
        Upn = 0,
        UidAndNetbios = 1,
        SamAccountName = 2,
        NetBIOSAndUid = 3,
        SID = 4,
        DistinguishedName = 5
    }
}
