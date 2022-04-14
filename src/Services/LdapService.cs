//Copyright(c) 2022 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-ldap-adapter/blob/main/LICENSE.md

using MultiFactor.Ldap.Adapter.Core;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MultiFactor.Ldap.Adapter.Services
{
    public class LdapService
    {
        //must not be mixed with proxied messages ids
        private int _messageId = Int32.MaxValue - 9999;

        private ILogger _logger;

        public LdapService(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IList<string>> GetAllGroups(Stream ldapConnectedStream, string userName)
        {
            if (!IsDistinguishedName(userName))
            {
                _logger.Error($"Can't query groups for user {userName}. Expected DistinguishedName.");
                return new string[0];
            }
            
            var request = CreateMemberOfRequest(userName);
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

        private LdapPacket CreateMemberOfRequest(string userName)
        {
            var packet = new LdapPacket(_messageId++);

            var baseDn = BaseDn(userName);

            var searchRequest = new LdapAttribute(LdapOperation.SearchRequest);
            searchRequest.ChildAttributes.Add(new LdapAttribute(UniversalDataType.OctetString, baseDn)); //base dn
            searchRequest.ChildAttributes.Add(new LdapAttribute(UniversalDataType.Enumerated, (byte)2));    //scope: subtree
            searchRequest.ChildAttributes.Add(new LdapAttribute(UniversalDataType.Enumerated, (byte)0));    //aliases: never
            searchRequest.ChildAttributes.Add(new LdapAttribute(UniversalDataType.Integer, (byte)255));     //size limit: 255
            searchRequest.ChildAttributes.Add(new LdapAttribute(UniversalDataType.Integer, (byte)60));      //time limit: 60
            searchRequest.ChildAttributes.Add(new LdapAttribute(UniversalDataType.Boolean, true));          //typesOnly: true

            var filter = new LdapAttribute(9);

            filter.ChildAttributes.Add(new LdapAttribute(1, "1.2.840.113556.1.4.1941"));    //AD filter
            filter.ChildAttributes.Add(new LdapAttribute(2, "member"));
            filter.ChildAttributes.Add(new LdapAttribute(3, userName)); 
            filter.ChildAttributes.Add(new LdapAttribute(4, (byte)0));

            searchRequest.ChildAttributes.Add(filter);

            packet.ChildAttributes.Add(searchRequest);

            var attrList = new LdapAttribute(UniversalDataType.Sequence);
            attrList.ChildAttributes.Add(new LdapAttribute(UniversalDataType.OctetString, "distinguishedName"));

            searchRequest.ChildAttributes.Add(attrList);

            return packet;
        }

        private static void Dump(LdapAttribute attr, int depth)
        {
            var tab = new string('\t', depth);

            //var value = string.Empty;
            //if (attr..Value is byte[] && attr.DataType == UniversalDataType.OctetString || attr.DataType == null)
            //{
            //    //value = Utils.ByteArrayToString(attr.Value);
            //    value = Encoding.UTF8.GetString(attr.Value);
            //}

            Console.WriteLine($"{tab}{attr.LdapOperation} Class:{attr.Class} ContextType:{attr.ContextType} DataType:{attr.DataType} Value: {attr.GetValue()} ({attr.Value.Length})");

            foreach (var child in attr.ChildAttributes)
            {
                Dump(child, depth + 1);
            }
        }

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

        /// <summary>
        /// DC part from DN
        /// </summary>
        public static string BaseDn(string dn)
        {
            var ncs = dn.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            var baseDn = ncs.Where(nc => nc.ToLower().StartsWith("dc="));
            return string.Join(",", baseDn);
        }

        /// <summary>
        /// Extracts CN from DN
        /// </summary>
        private string DnToCn(string dn)
        {
            return dn.Split(',')[0].Split(new[] { '=' })[1];
        }

        private bool IsDistinguishedName(string name)
        {
            return name.Contains(",");
        }
    }
}