using Serilog.Core;
using Serilog;
using System.IO;
using ILogger = Serilog.ILogger;
using Serilog.Formatting.Compact;
using Serilog.Formatting;

namespace MultiFactor.Ldap.Adapter.Core.Logging
{
    public class LoggerFactory
    {
        public ILogger CreateLogger(string logFormat, string logLevel, LoggingLevelSwitch levelSwitch)
        {
            var loggerConfiguration = new LoggerConfiguration()
                .MinimumLevel.ControlledBy(levelSwitch);

            var formatter = GetLogFormatter(logFormat);
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

            return loggerConfiguration.CreateLogger();
        }

        private ITextFormatter GetLogFormatter(string format)
        {
            switch (format?.ToLower())
            {
                case "json":
                    return new RenderedCompactJsonFormatter();
                default:
                    return null;
            }
        }
    }
}
