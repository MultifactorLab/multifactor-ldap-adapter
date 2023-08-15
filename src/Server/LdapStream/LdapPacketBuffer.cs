using System.Collections.Generic;

namespace MultiFactor.Ldap.Adapter.Server.LdapStream
{
    public struct LdapPacketBuffer
    {
        public bool PacketValid;
        public byte[] Data;
    }
}
