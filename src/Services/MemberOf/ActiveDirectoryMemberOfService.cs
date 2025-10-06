using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MultiFactor.Ldap.Adapter.Core;

namespace MultiFactor.Ldap.Adapter.Services.MemberOf;

public class ActiveDirectoryMemberOfService : IMemberOfService
{
    public async Task<bool> IsMemberOf(Stream ldapConnectedStream, LdapProfile profile, string groupDn, int messageId)
    {
        var filter = GetFilter(groupDn);
        var request = BuildMemberOfRequest(profile.Dn, filter, messageId);
        var requestData = request.GetBytes();
        await ldapConnectedStream.WriteAsync(requestData, 0, requestData.Length);
        var users = new List<string>();
        LdapPacket packet;
        while ((packet = await LdapPacket.ParsePacket(ldapConnectedStream)) != null)
        {
            users.AddRange(GetSearchResult(packet));
        }
            
        return users.Any();
    }
    
    private LdapAttribute[] GetFilter(string groupDn)
    {
        return new[]
        {
            new LdapAttribute((byte)LdapFilterChoice.extensibleMatch) 
            {
                ChildAttributes = 
                {
                    new LdapAttribute(1, "1.2.840.113556.1.4.1941"),
                    new LdapAttribute(2, "memberof"),
                    new LdapAttribute(3, groupDn),
                    new LdapAttribute(4, (byte)0)
                }
            }
        };
    }
    
    private LdapPacket BuildMemberOfRequest(string userName, LdapAttribute[] memberFilter, int messageId)
    {
        var packet = new LdapPacket(messageId);

        var searchRequest = new LdapAttribute(LdapOperation.SearchRequest);
        searchRequest.ChildAttributes.Add(new LdapAttribute(UniversalDataType.OctetString, userName));    //base dn
        searchRequest.ChildAttributes.Add(new LdapAttribute(UniversalDataType.Enumerated, (byte)2));    //scope: subtree
        searchRequest.ChildAttributes.Add(new LdapAttribute(UniversalDataType.Enumerated, (byte)0));    //aliases: never
        searchRequest.ChildAttributes.Add(new LdapAttribute(UniversalDataType.Integer, (byte)0));       //size limit: unset
        searchRequest.ChildAttributes.Add(new LdapAttribute(UniversalDataType.Integer, (byte)60));      //time limit: 60
        searchRequest.ChildAttributes.Add(new LdapAttribute(UniversalDataType.Boolean, true));          //typesOnly: true

        foreach (var attribute in memberFilter)
        {
            searchRequest.ChildAttributes.Add(attribute);
        }

        packet.ChildAttributes.Add(searchRequest);

        var attrList = new LdapAttribute(UniversalDataType.Sequence);
        attrList.ChildAttributes.Add(new LdapAttribute(UniversalDataType.OctetString, "distinguishedName"));

        searchRequest.ChildAttributes.Add(attrList);

        return packet;
    }
    
    private IEnumerable<string> GetSearchResult(LdapPacket packet)
    {
        var searchResults = new List<string>();

        foreach (var searchResultEntry in packet.ChildAttributes.FindAll(attr => attr.LdapOperation == LdapOperation.SearchResultEntry))
        {
            if (searchResultEntry.ChildAttributes.Count > 0)
            {
                var result = searchResultEntry.ChildAttributes[0].GetValue<string>();
                searchResults.Add(result);
            }
        }

        return searchResults;
    }
}