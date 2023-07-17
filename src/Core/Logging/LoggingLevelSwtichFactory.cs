using Serilog.Core;
using Serilog.Events;

namespace MultiFactor.Ldap.Adapter.Core.Logging
{
    public static class LoggingLevelSwtichFactory
    {
        public static void SetLogLevel(this LoggingLevelSwitch levelSwitch, string level)
        {
            switch (level)
            {
                case "Debug":
                    levelSwitch.MinimumLevel = LogEventLevel.Debug;
                    break;
                case "Info":
                    levelSwitch.MinimumLevel = LogEventLevel.Information;
                    break;
                case "Warn":
                    levelSwitch.MinimumLevel = LogEventLevel.Warning;
                    break;
                case "Error":
                    levelSwitch.MinimumLevel = LogEventLevel.Error;
                    break;
            }
        }
    }
}
