using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MultiFactor.Ldap.Adapter.Core;

namespace MultiFactor.Ldap.Adapter.Services.MemberOf;

public class FreeIpaMemberOfService : IMemberOfService
{
    private List<string> _groups = new List<string>();
    
    public async Task<bool> IsMemberOf(Stream ldapConnectedStream, LdapProfile profile, string groupDn, int messageId)
    {
        var groups = await GetUserGroups(ldapConnectedStream, profile, messageId);
        return groups.Any(g => g == LdapService.FormatDn(groupDn));
    }

    private async Task<List<string>> GetUserGroups(Stream ldapConnectedStream, LdapProfile profile, int messageId)
    {
        if (_groups.Count > 0)
            return _groups;
        
        var filter = GetFilter(profile.Dn);
        var request = BuildMemberOfRequest(profile.Dn, filter, messageId);
        var requestData = request.GetBytes();
        await ldapConnectedStream.WriteAsync(requestData, 0, requestData.Length);
        LdapPacket packet;
        while ((packet = await LdapPacket.ParsePacket(ldapConnectedStream)) != null)
        {
            _groups.AddRange(GetSearchResult(packet));
        }
        
        return _groups;
    }

    private LdapPacket BuildMemberOfRequest(string userName, LdapAttribute[] memberFilter, int messageId)
    {
        var packet = new LdapPacket(messageId);

        var baseDn = LdapProfile.GetBaseDn(userName);

        var searchRequest = new LdapAttribute(LdapOperation.SearchRequest);
        searchRequest.ChildAttributes.Add(new LdapAttribute(UniversalDataType.OctetString, baseDn));    //base dn
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
    
    
    private LdapAttribute[] GetFilter(string userName)
    {
        return new[] 
        {
            new LdapAttribute((byte)LdapFilterChoice.equalityMatch) 
            {
                ChildAttributes = 
                {
                    new LdapAttribute(UniversalDataType.OctetString, "member"),
                    new LdapAttribute(UniversalDataType.OctetString, userName) 
                }
            }
        };
    }
    
    private IEnumerable<string> GetSearchResult(LdapPacket packet)
    {
        var searchResults = new List<string>();

        foreach (var searchResultEntry in packet.ChildAttributes.FindAll(attr => attr.LdapOperation == LdapOperation.SearchResultEntry))
        {
            if (searchResultEntry.ChildAttributes.Count > 0)
            {
                var result = searchResultEntry.ChildAttributes[0].GetValue<string>();
                searchResults.Add(LdapService.FormatDn(result));
            }
        }

        return searchResults;
    }
}