//Copyright(c) 2021 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-ldap-adapter/blob/main/LICENSE.md

namespace MultiFactor.Ldap.Adapter.Server;

public interface ILdapServer
{
    bool Enabled { get; }
    void Start();
    void Stop();
}