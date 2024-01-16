namespace MultiFactor.Ldap.Adapter.Core.NameResolving
{
    public class NetbiosDomainName
    {
        public string Domain { get; set; }
        public string NetbiosName { get; set; }

        public NetbiosDomainName() { }
        public NetbiosDomainName(string domain, string netbiosName) 
        {
            Domain = domain;
            NetbiosName = netbiosName;
        }
    }
}
