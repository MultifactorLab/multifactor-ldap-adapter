//Copyright(c) 2022 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-ldap-adapter/blob/main/LICENSE.md

using MultiFactor.Ldap.Adapter.Configuration;
using MultiFactor.Ldap.Adapter.Core;
using MultiFactor.Ldap.Adapter.Core.NameResolving;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MultiFactor.Ldap.Adapter.Services
{
    public class LdapService
    {
        //must not repeat proxied messages ids
        private int _messageId = Int32.MaxValue - 9999;
        private readonly ClientConfiguration _config;

        public LdapService(ClientConfiguration config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        #region requests builders

        private LdapPacket BuildRootDSERequest()
        {
            var packet = new LdapPacket(_messageId++);

            var searchRequest = new LdapAttribute(LdapOperation.SearchRequest);
            searchRequest.ChildAttributes.Add(new LdapAttribute(UniversalDataType.OctetString, string.Empty));      //base dn
            searchRequest.ChildAttributes.Add(new LdapAttribute(UniversalDataType.Enumerated, (byte)0));            //scope: base
            searchRequest.ChildAttributes.Add(new LdapAttribute(UniversalDataType.Enumerated, (byte)0));            //aliases: never
            searchRequest.ChildAttributes.Add(new LdapAttribute(UniversalDataType.Integer, (byte)1));               //size limit: 1
            searchRequest.ChildAttributes.Add(new LdapAttribute(UniversalDataType.Integer, (byte)60));              //time limit: 60
            searchRequest.ChildAttributes.Add(new LdapAttribute(UniversalDataType.Boolean, false));                 //typesOnly: false

            searchRequest.ChildAttributes.Add(new LdapAttribute(7, "objectClass")); //filter

            packet.ChildAttributes.Add(searchRequest);

            var attrList = new LdapAttribute(UniversalDataType.Sequence);

            attrList.ChildAttributes.Add(new LdapAttribute(UniversalDataType.OctetString, "defaultNamingContext"));

            searchRequest.ChildAttributes.Add(attrList);

            return packet;
        }

        private LdapPacket BuildLoadProfileRequest(string userName, string baseDn)
        {
            var packet = new LdapPacket(_messageId++);

            var searchRequest = new LdapAttribute(LdapOperation.SearchRequest);
            searchRequest.ChildAttributes.Add(new LdapAttribute(UniversalDataType.OctetString, baseDn));    //base dn
            searchRequest.ChildAttributes.Add(new LdapAttribute(UniversalDataType.Enumerated, (byte)2));    //scope: subtree
            searchRequest.ChildAttributes.Add(new LdapAttribute(UniversalDataType.Enumerated, (byte)3));    //aliases: never
            searchRequest.ChildAttributes.Add(new LdapAttribute(UniversalDataType.Integer, (byte)255));     //size limit: 255
            searchRequest.ChildAttributes.Add(new LdapAttribute(UniversalDataType.Integer, (byte)60));      //time limit: 60
            searchRequest.ChildAttributes.Add(new LdapAttribute(UniversalDataType.Boolean, false));         //typesOnly: false

            var id = LdapUserIdentity.Parse(userName);

            var and = new LdapAttribute((byte)LdapFilterChoice.and);

            var eq1 = new LdapAttribute((byte)LdapFilterChoice.equalityMatch);
            var bindDnDefined = !string.IsNullOrEmpty(_config.LdapBaseDn);
            if (bindDnDefined)
            {
                eq1.ChildAttributes.Add(new LdapAttribute(UniversalDataType.OctetString, "uid"));
                eq1.ChildAttributes.Add(new LdapAttribute(UniversalDataType.OctetString, id.GetUid()));
            }
            else 
            {
                eq1.ChildAttributes.Add(new LdapAttribute(UniversalDataType.OctetString, id.Type.ToString()));
                eq1.ChildAttributes.Add(new LdapAttribute(UniversalDataType.OctetString, id.Name));
            }

            var eq2 = new LdapAttribute((byte)LdapFilterChoice.equalityMatch);
            eq2.ChildAttributes.Add(new LdapAttribute(UniversalDataType.OctetString, "objectClass"));
            if (bindDnDefined)
            {
                eq2.ChildAttributes.Add(new LdapAttribute(UniversalDataType.OctetString, "person"));
            }
            else
            {
                eq2.ChildAttributes.Add(new LdapAttribute(UniversalDataType.OctetString, "user"));
            }

            and.ChildAttributes.Add(eq1);
            and.ChildAttributes.Add(eq2);

            searchRequest.ChildAttributes.Add(and);

            packet.ChildAttributes.Add(searchRequest);

            var attrList = new LdapAttribute(UniversalDataType.Sequence);
            attrList.ChildAttributes.Add(new LdapAttribute(UniversalDataType.OctetString, "uid"));
            attrList.ChildAttributes.Add(new LdapAttribute(UniversalDataType.OctetString, "sAMAccountName"));
            attrList.ChildAttributes.Add(new LdapAttribute(UniversalDataType.OctetString, "UserPrincipalName"));
            attrList.ChildAttributes.Add(new LdapAttribute(UniversalDataType.OctetString, "DisplayName"));
            attrList.ChildAttributes.Add(new LdapAttribute(UniversalDataType.OctetString, "mail"));
            attrList.ChildAttributes.Add(new LdapAttribute(UniversalDataType.OctetString, "memberOf"));

            searchRequest.ChildAttributes.Add(attrList);

            return packet;
        }

        private LdapAttribute[] GetADMemberOfFilter(string userName) 
        {
            return new[]
            {
                new LdapAttribute((byte)LdapFilterChoice.extensibleMatch) 
                {
                    ChildAttributes = 
                    {
                        new LdapAttribute(1, "1.2.840.113556.1.4.1941"),
                        new LdapAttribute(2, "member"),
                        new LdapAttribute(3, userName),
                        new LdapAttribute(4, (byte)0)
                    }
                }
            };
        }

        private LdapAttribute[] GetFreeIpaMemberOfFilter(string userName)
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

        private LdapPacket BuildMemberOfRequest(string userName, LdapAttribute[] memberFilter)
        {
            var packet = new LdapPacket(_messageId++);

            var baseDn = LdapProfile.GetBaseDn(userName);

            var searchRequest = new LdapAttribute(LdapOperation.SearchRequest);
            searchRequest.ChildAttributes.Add(new LdapAttribute(UniversalDataType.OctetString, baseDn));    //base dn
            searchRequest.ChildAttributes.Add(new LdapAttribute(UniversalDataType.Enumerated, (byte)2));    //scope: subtree
            searchRequest.ChildAttributes.Add(new LdapAttribute(UniversalDataType.Enumerated, (byte)0));    //aliases: never
            searchRequest.ChildAttributes.Add(new LdapAttribute(UniversalDataType.Integer, (byte)255));     //size limit: 255
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

        public LdapPacket BuildGetPartitions(string baseDn)
        {
            var packet = new LdapPacket(_messageId++);

            var searchRequest = new LdapAttribute(LdapOperation.SearchRequest);
            searchRequest.ChildAttributes.Add(new LdapAttribute(UniversalDataType.OctetString, "CN=Partitions,CN=Configuration," + baseDn));    // base dn
            searchRequest.ChildAttributes.Add(new LdapAttribute(UniversalDataType.Enumerated, (byte)2));    // scope: subtree
            searchRequest.ChildAttributes.Add(new LdapAttribute(UniversalDataType.Enumerated, (byte)3));    // aliases: never
            searchRequest.ChildAttributes.Add(new LdapAttribute(UniversalDataType.Integer, (Int32)1000));     // size limit: 255
            searchRequest.ChildAttributes.Add(new LdapAttribute(UniversalDataType.Integer, (byte)60));      // time limit: 60
            searchRequest.ChildAttributes.Add(new LdapAttribute(UniversalDataType.Boolean, false));         //typesOnly: false

            var and = new LdapAttribute((byte)LdapFilterChoice.and);

            var eq1 = new LdapAttribute((byte)LdapFilterChoice.equalityMatch);
            var present = new LdapAttribute((byte)LdapFilterChoice.present, "netbiosname");

            and.ChildAttributes.Add(eq1);
            and.ChildAttributes.Add(present);

            searchRequest.ChildAttributes.Add(and);

            eq1.ChildAttributes.Add(new LdapAttribute(UniversalDataType.OctetString, "objectcategory"));
            eq1.ChildAttributes.Add(new LdapAttribute(UniversalDataType.OctetString, "crossref"));

            packet.ChildAttributes.Add(searchRequest);

            var attrList = new LdapAttribute(UniversalDataType.Sequence);
            attrList.ChildAttributes.Add(new LdapAttribute(UniversalDataType.OctetString, "netbiosname"));
            attrList.ChildAttributes.Add(new LdapAttribute(UniversalDataType.OctetString, "dnsRoot"));
            searchRequest.ChildAttributes.Add(attrList);
            return packet;
        }


        public LdapPacket BuildResolveProfileRequest(string name, string baseDn)
        {
            var packet = new LdapPacket(_messageId++);

            var searchRequest = new LdapAttribute(LdapOperation.SearchRequest);
            searchRequest.ChildAttributes.Add(new LdapAttribute(UniversalDataType.OctetString, baseDn));    // base dn
            searchRequest.ChildAttributes.Add(new LdapAttribute(UniversalDataType.Enumerated, (byte)2));    // scope: subtree
            searchRequest.ChildAttributes.Add(new LdapAttribute(UniversalDataType.Enumerated, (byte)0));    // aliases: never
            searchRequest.ChildAttributes.Add(new LdapAttribute(UniversalDataType.Integer, (Int32)1000));     // size limit: 255
            searchRequest.ChildAttributes.Add(new LdapAttribute(UniversalDataType.Integer, (byte)60));      // time limit: 60
            searchRequest.ChildAttributes.Add(new LdapAttribute(UniversalDataType.Boolean, false));         //typesOnly: false

            var and = new LdapAttribute((byte)LdapFilterChoice.and);
            var or = new LdapAttribute((byte)LdapFilterChoice.or);

            var sAMAccountNameEq = new LdapAttribute((byte)LdapFilterChoice.equalityMatch);
            sAMAccountNameEq.ChildAttributes.Add(new LdapAttribute(UniversalDataType.OctetString, "sAMAccountName"));
            var sAMAccountNameRegex = new Regex("@[^@]+$");
            var netbiosRegex = new Regex(@"[^.]+\\");
            sAMAccountNameEq.ChildAttributes.Add(
                new LdapAttribute(UniversalDataType.OctetString,
                        netbiosRegex.Replace(sAMAccountNameRegex.Replace(name, ""), ""))
            );

            var upnEq = new LdapAttribute((byte)LdapFilterChoice.equalityMatch);
            upnEq.ChildAttributes.Add(new LdapAttribute(UniversalDataType.OctetString, "UserPrincipalName"));
            upnEq.ChildAttributes.Add(new LdapAttribute(UniversalDataType.OctetString, name));


            var dnEq = new LdapAttribute((byte)LdapFilterChoice.equalityMatch);
            dnEq.ChildAttributes.Add(new LdapAttribute(UniversalDataType.OctetString, "distinguishedName"));
            dnEq.ChildAttributes.Add(new LdapAttribute(UniversalDataType.OctetString, name));


            or.ChildAttributes.Add(sAMAccountNameEq);
            or.ChildAttributes.Add(upnEq);
            or.ChildAttributes.Add(dnEq);

            var objectClassEq = new LdapAttribute((byte)LdapFilterChoice.equalityMatch);

            objectClassEq.ChildAttributes.Add(new LdapAttribute(UniversalDataType.OctetString, "objectClass"));
            objectClassEq.ChildAttributes.Add(new LdapAttribute(UniversalDataType.OctetString, "user"));

            and.ChildAttributes.Add(objectClassEq);
            and.ChildAttributes.Add(or);

            searchRequest.ChildAttributes.Add(and);

            var attrList = new LdapAttribute(UniversalDataType.Sequence);
            attrList.ChildAttributes.Add(new LdapAttribute(UniversalDataType.OctetString, "uid"));
            attrList.ChildAttributes.Add(new LdapAttribute(UniversalDataType.OctetString, "sAMAccountName"));
            attrList.ChildAttributes.Add(new LdapAttribute(UniversalDataType.OctetString, "UserPrincipalName"));
            attrList.ChildAttributes.Add(new LdapAttribute(UniversalDataType.OctetString, "DisplayName"));
            attrList.ChildAttributes.Add(new LdapAttribute(UniversalDataType.OctetString, "mail"));
            attrList.ChildAttributes.Add(new LdapAttribute(UniversalDataType.OctetString, "memberOf"));

            searchRequest.ChildAttributes.Add(attrList);

            packet.ChildAttributes.Add(searchRequest);

            return packet;
        }

        #endregion

        #region queries

        public async Task<string> GetDefaultNamingContext(Stream ldapConnectedStream)
        {
            var request = BuildRootDSERequest();
            var requestData = request.GetBytes();

            await ldapConnectedStream.WriteAsync(requestData, 0, requestData.Length);

            string defaultNamingContext = null;

            LdapPacket packet;
            while ((packet = await LdapPacket.ParsePacket(ldapConnectedStream)) != null)
            {
                var searchResult = packet.ChildAttributes.SingleOrDefault(c => c.LdapOperation == LdapOperation.SearchResultEntry);
                if (searchResult != null)
                {
                    var attrs = searchResult.ChildAttributes[1];
                    var entry = GetEntry(attrs.ChildAttributes[0]);

                    defaultNamingContext = entry.Values.FirstOrDefault();
                }
            }

            return defaultNamingContext;
        }

        public async Task<string> GetBaseDn(Stream ldapConnectedStream, string userName)
        {
            string baseDn;
            // request netbios name?
            if (GetIdentityType(userName) == IdentityType.DistinguishedName)
            {
                //if userName is distinguishedName, get basedn from it
                baseDn = LdapProfile.GetBaseDn(userName);
            }
            else
            {
                //else query defaultNamingContext from ldap
                baseDn = await GetDefaultNamingContext(ldapConnectedStream);
            }
            return baseDn;
        }
        public async Task<LdapProfile> LoadProfile(Stream ldapConnectedStream, string userName, string baseDn)
        {
            var request = BuildLoadProfileRequest(userName, baseDn);
            var requestData = request.GetBytes();

            await ldapConnectedStream.WriteAsync(requestData, 0, requestData.Length);

            LdapProfile profile = null;
            LdapPacket packet;

            while ((packet = await LdapPacket.ParsePacket(ldapConnectedStream)) != null)
            {
                var searchResult = packet.ChildAttributes.SingleOrDefault(c => c.LdapOperation == LdapOperation.SearchResultEntry);
                if (searchResult != null)
                {
                    profile ??= new LdapProfile();

                    var dn = searchResult.ChildAttributes[0].GetValue<string>();
                    var attrs = searchResult.ChildAttributes[1];

                    profile.Dn = dn;

                    foreach (var valueAttr in attrs.ChildAttributes)
                    {
                        var entry = GetEntry(valueAttr);

                        switch (entry.Name)
                        {
                            case "uid":
                                profile.Uid = entry.Values.FirstOrDefault();    //openldap, freeipa
                                break;
                            case "sAMAccountName":
                                profile.Uid = entry.Values.FirstOrDefault();    //ad
                                break;
                            case "displayName":
                                profile.DisplayName = entry.Values.FirstOrDefault();
                                break;
                            case "userPrincipalName":
                                profile.Upn = entry.Values.FirstOrDefault();
                                break;
                            case "mail":
                                profile.Email = entry.Values.FirstOrDefault();
                                break;
                            case "memberOf":
                                profile.MemberOf.AddRange(entry.Values.Select(v => DnToCn(v)));
                                break;
                        }
                    }
                }
            }

            return profile;
        }

        public async Task<List<string>> GetAllGroups(Stream ldapConnectedStream, LdapProfile profile, ClientConfiguration clientConfiguration)
        {
            if (!clientConfiguration.LoadActiveDirectoryNestedGroups)
            {
                return profile.MemberOf;
            }
            
            var memberOfFilter = string.IsNullOrEmpty(clientConfiguration.LdapBaseDn) 
                ? GetADMemberOfFilter(profile.Dn)
                : GetFreeIpaMemberOfFilter(profile.Dn);

            var request = BuildMemberOfRequest(profile.Dn, memberOfFilter);
            var requestData = request.GetBytes();
            await ldapConnectedStream.WriteAsync(requestData, 0, requestData.Length);

            var groups = new List<string>();

            LdapPacket packet;
            while ((packet = await LdapPacket.ParsePacket(ldapConnectedStream)) != null)
            {
                groups.AddRange(GetGroups(packet));
            }

            return groups;
        }

        #endregion 

        private IEnumerable<string> GetGroups(LdapPacket packet)
        {
            var groups = new List<string>();

            foreach (var searchResultEntry in packet.ChildAttributes.FindAll(attr => attr.LdapOperation == LdapOperation.SearchResultEntry))
            {
                if (searchResultEntry.ChildAttributes.Count > 0)
                {
                    var group = searchResultEntry.ChildAttributes[0].GetValue<string>();
                    groups.Add(DnToCn(group));
                }
            }

            return groups;
        }

        public async Task<NetbiosDomainName[]> GetDomains(Stream serverStream, string baseDn)
        {
            var request = BuildGetPartitions(baseDn);
            await serverStream.WriteAsync(request.GetBytes());
            LdapPacket packet;
            var result = new List<NetbiosDomainName>();
            while ((packet = await LdapPacket.ParsePacket(serverStream)) != null)
            {
                var searchResult = packet.ChildAttributes.SingleOrDefault(c => c.LdapOperation == LdapOperation.SearchResultEntry);
                if (searchResult != null)
                {
                    var attrs = searchResult.ChildAttributes[1];
                    var domain = new NetbiosDomainName();
                    foreach (var valueAttr in attrs.ChildAttributes)
                    {
                        var entry = GetEntry(valueAttr);

                        if (entry.Name == "nETBIOSName")
                        {
                            domain.NetbiosName = entry.Values.First();
                        }

                        if (entry.Name == "dnsRoot")
                        {
                            domain.Domain = entry.Values.First();
                        }
                    }
                    result.Add(domain);
                }
            }

            return result.ToArray();
        }

        public async Task<LdapProfile> ResolveProfile(Stream serverStream, string name, string baseDn)
        {
            var request = BuildResolveProfileRequest(name, baseDn);
            await serverStream.WriteAsync(request.GetBytes());
            var result = new List<NetbiosDomainName>();

            LdapProfile profile = null;
            LdapPacket packet;

            while ((packet = await LdapPacket.ParsePacket(serverStream)) != null)
            {
                var searchResult = packet.ChildAttributes.SingleOrDefault(c => c.LdapOperation == LdapOperation.SearchResultEntry);
                if (searchResult != null)
                {
                    profile ??= new LdapProfile();

                    var dn = searchResult.ChildAttributes[0].GetValue<string>();
                    var attrs = searchResult.ChildAttributes[1];

                    profile.Dn = dn;

                    foreach (var valueAttr in attrs.ChildAttributes)
                    {
                        var entry = GetEntry(valueAttr);

                        switch (entry.Name)
                        {
                            case "uid":
                                profile.Uid = entry.Values.FirstOrDefault();    //openldap, freeipa
                                break;
                            case "sAMAccountName":
                                profile.Uid = entry.Values.FirstOrDefault();    //ad
                                break;
                            case "userPrincipalName":
                                profile.Upn = entry.Values.FirstOrDefault();
                                break;
                        }
                    }
                }
            }

            return profile;
        }

        /// <summary>
        /// Extracts CN from DN
        /// </summary>
        private string DnToCn(string dn)
        {
            return dn.Split(',')[0].Split(new[] { '=' })[1];
        }

        public static IdentityType GetIdentityType(string userName)
        {
            if (userName.Contains("@")) return IdentityType.UserPrincipalName;
            if (userName.Contains("CN=", StringComparison.OrdinalIgnoreCase)) return IdentityType.DistinguishedName;
            return IdentityType.sAMAccountName;
        }

        private static LdapSearchResultEntry GetEntry(LdapAttribute ldapAttribute)
        {
            var name = ldapAttribute.ChildAttributes[0].GetValue<string>();
            var ret = new LdapSearchResultEntry { Name = name, Values = new List<string>() };

            if (ldapAttribute.ChildAttributes.Count > 1)
            {
                foreach (var valueAttribute in ldapAttribute.ChildAttributes[1].ChildAttributes)
                {
                    ret.Values.Add(valueAttribute.GetValue()?.ToString());
                }
            }

            return ret;
        }

        private class LdapSearchResultEntry
        {
            public string Name { get; set; }
            public IList<string> Values { get; set; }
        }
    }
}