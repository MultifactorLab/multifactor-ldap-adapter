using System;
using System.IO;
using System.Reflection;

namespace MultiFactor.Ldap.Adapter.Core;

internal static class ApplicationVariablesFactory
{
    public static ApplicationVariables Create()
    {
        return new ApplicationVariables
        {
            AppVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString()
        };
    }
}
