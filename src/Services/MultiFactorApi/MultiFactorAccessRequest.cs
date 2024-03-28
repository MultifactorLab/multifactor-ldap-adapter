using System.Collections.Generic;

namespace MultiFactor.Ldap.Adapter.Services.MultiFactorApi
{
    public class MultiFactorAccessRequest
    {
        public List<string> ApiUrls { get; set; }

        public string Identity { get; set; }

        public string Auth { get; set; }

        public bool BypassSecondFactor { get; set; }
    }
}
