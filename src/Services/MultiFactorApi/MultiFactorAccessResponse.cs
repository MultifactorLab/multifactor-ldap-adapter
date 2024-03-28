//Copyright(c) 2021 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-ldap-adapter/blob/main/LICENSE.md

namespace MultiFactor.Ldap.Adapter.Services.MultiFactorApi
{
    public class MultiFactorAccessResponse
    {
        public string Id { get; set; }
        public string Identity { get; set; }
        public string Phone { get; set; }
        public string Status { get; set; }
        public string ReplyMessage { get; set; }
        public bool Bypassed { get; set; }
        public string Authenticator { get; set; }
        public string Account { get; set; }

        public bool Granted => Status == "Granted";
        public bool Denied => Status == "Denied";

        public static MultiFactorAccessResponse Bypass = new MultiFactorAccessResponse { Status = "Granted", Bypassed = true };

        public static MultiFactorAccessResponse Empty = new MultiFactorAccessResponse { Bypassed = false };
    }
}
