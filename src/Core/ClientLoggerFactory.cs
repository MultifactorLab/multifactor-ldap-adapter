using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MultiFactor.Ldap.Adapter.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog;
using System.IO;
using ILogger = Serilog.ILogger;
using Serilog.Formatting.Compact;
using Serilog.Formatting;
using System.Collections.Generic;

namespace MultiFactor.Ldap.Adapter.Core
{
    public class ClientLoggerFactory
    {
        private readonly ILogger _globalLogger;
        private Dictionary<string,  ILogger> _loggerMap = new Dictionary<string, ILogger>();
        public ClientLoggerFactory(ILogger globalLogger)
        {
            _globalLogger = globalLogger;
        }
        
        public ILogger GetLogger(ClientConfiguration configuration)
        {
            if(string.IsNullOrEmpty(configuration.LogLevel))
            {
                return _globalLogger;
            }
            if(!_loggerMap.ContainsKey(configuration.Name))
            {
                _loggerMap[configuration.Name] = CreateLogger(configuration);
            }
            return _loggerMap[configuration.Name];
        }
        
        private ILogger CreateLogger(ClientConfiguration configuration)
        {
            var levelSwitch = new LoggingLevelSwitch(LogEventLevel.Information);
            var loggerConfiguration = new LoggerConfiguration()
                .MinimumLevel.ControlledBy(levelSwitch);

            var formatter = GetLogFormatter(configuration);
            if (formatter != null)
            {
                loggerConfiguration
                    .WriteTo.Console(formatter)
                    .WriteTo.File(formatter, $"{Core.Constants.ApplicationPath}logs{Path.DirectorySeparatorChar}log-.txt", rollingInterval: RollingInterval.Day);
            }
            else
            {
                loggerConfiguration
                    .WriteTo.Console()
                    .WriteTo.File($"{Core.Constants.ApplicationPath}logs{Path.DirectorySeparatorChar}log-.txt", rollingInterval: RollingInterval.Day);
            }
            SetLogLevel(configuration.LogLevel, levelSwitch);
            var logger = loggerConfiguration.CreateLogger();
            logger.Information($"Logging level {levelSwitch.MinimumLevel} for client {configuration.Name}");
            return logger;
        }

        private ITextFormatter GetLogFormatter(ClientConfiguration configuration)
        {
            var format = configuration.LogFormat;
            switch (format?.ToLower())
            {
                case "json":
                    return new RenderedCompactJsonFormatter();
                default:
                    return null;
            }
        }

        private void SetLogLevel(string level, LoggingLevelSwitch levelSwitch)
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
