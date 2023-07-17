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

namespace MultiFactor.Ldap.Adapter.Core.Logging
{
    public class ClientLoggerProvider
    {
        private ILogger _globalLogger = null;
        private Dictionary<string, ILogger> _loggerMap = new Dictionary<string, ILogger>();

        public ILogger GetLogger(LoggingLevelSwitch loggingLevelSwitch)
        {
            if (_globalLogger == null)
            {
                _globalLogger = CreateLogger(null, loggingLevelSwitch);
            }
            return _globalLogger;
        }

        public ILogger GetLogger(ClientConfiguration configuration)
        {
            if (string.IsNullOrEmpty(configuration.LogLevel))
            {
                return _globalLogger;
            }
            if (!_loggerMap.ContainsKey(configuration.Name))
            {
                var levelSwitch = new LoggingLevelSwitch(LogEventLevel.Information);
                _loggerMap[configuration.Name] = CreateLogger(configuration, levelSwitch);
            }
            return _loggerMap[configuration.Name];
        }

        private ILogger CreateLogger(ClientConfiguration configuration, LoggingLevelSwitch levelSwitch)
        {
            var loggerConfiguration = new LoggerConfiguration()
                .MinimumLevel.ControlledBy(levelSwitch);

            var formatter = GetLogFormatter(configuration);
            if (formatter != null)
            {
                loggerConfiguration
                    .WriteTo.Console(formatter)
                    .WriteTo.File(formatter, $"{Constants.ApplicationPath}logs{Path.DirectorySeparatorChar}log-.txt", rollingInterval: RollingInterval.Day);
            }
            else
            {
                loggerConfiguration
                    .WriteTo.Console()
                    .WriteTo.File($"{Constants.ApplicationPath}logs{Path.DirectorySeparatorChar}log-.txt", rollingInterval: RollingInterval.Day);
            }
            ILogger logger;
            if (configuration != null)
            {
                SetLogLevel(configuration.LogLevel, levelSwitch);
                logger = loggerConfiguration.CreateLogger();
                logger.Information($"Logging level {levelSwitch.MinimumLevel} for client {configuration.Name}");
            }
            else
            {
                logger = loggerConfiguration.CreateLogger();
            }
            return logger;
        }

        private ITextFormatter GetLogFormatter(ClientConfiguration configuration)
        {
            var format = configuration == null ? ServiceConfiguration.GetLogFormat() : configuration.LogFormat;
            switch (format?.ToLower())
            {
                case "json":
                    return new RenderedCompactJsonFormatter();
                default:
                    return null;
            }
        }

        public void SetLogLevel(string level, LoggingLevelSwitch levelSwitch)
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
