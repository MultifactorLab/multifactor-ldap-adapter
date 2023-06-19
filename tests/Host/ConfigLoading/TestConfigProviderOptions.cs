using System;

namespace MultiFactor.Ldap.Adapter.Tests.Fixtures.ConfigLoading
{
    internal class TestConfigProviderOptions
    {
        public string RootConfigFilePath { get; set; }
        public string[] ClientConfigFilePaths { get; set; } = Array.Empty<string>();
    }
}