//Copyright(c) 2021 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-ldap-adapter/blob/main/LICENSE.md

using System.Linq;
using System.Net;

namespace MultiFactor.Ldap.Adapter.Server
{
    public class RemoteEndPoint
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public bool UseTls { get; set; }
    
        public IPEndPoint GetIPEndPoint()
        {
            var ip = Dns.GetHostAddresses(Host)[0];
            return new IPEndPoint(ip, Port);
        }
    }
}
