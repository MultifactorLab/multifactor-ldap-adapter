using System.IO;
using System.Threading.Tasks;

namespace MultiFactor.Ldap.Adapter.Services.MemberOf;

public interface IMemberOfService
{
    Task<bool> IsMemberOf(Stream ldapConnectedStream, LdapProfile profile, string groupDn, int messageId);
}