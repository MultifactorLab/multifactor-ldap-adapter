using MultiFactor.Ldap.Adapter.Configuration;
using ILogger = Serilog.ILogger;
using Serilog;
using System.Collections.Concurrent;

namespace MultiFactor.Ldap.Adapter.Core.Logging
{
    public class ClientLoggerProvider
    {
        protected ConcurrentDictionary<string, ILogger> _loggerMap = new ConcurrentDictionary<string, ILogger>();

        public ILogger GetLogger(ClientConfiguration configuration)
        {
            if (string.IsNullOrEmpty(configuration.LogLevel))
            {
                return Log.Logger;
            }
            if (!_loggerMap.ContainsKey(configuration.Name))
            {
                var logger = LoggerFactory.CreateLogger(configuration.LogFormat, configuration.LogLevel);
                _loggerMap.TryAdd(configuration.Name, logger);
            }
            return _loggerMap[configuration.Name];
        }
    }
}
