using System;
using System.Threading;
using MultiFactor.Ldap.Adapter.Core.Logging;

namespace MultiFactor.Ldap.Adapter.Configuration;

public static class ConfigurationValueParser
{
    public static TimeSpan RecommendedTimeout { get; } = TimeSpan.FromSeconds(65);

    public static TimeSpan ParseTimeout(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return RecommendedTimeout;
        }

        var isForced = value.EndsWith('!');
        if (isForced)
        {
            value = value.TrimEnd('!');
        }

        if (!TimeSpan.TryParseExact(value,
                @"hh\:mm\:ss",
                null,
                System.Globalization.TimeSpanStyles.None,
                out var timeout))
        {
            StartupLogger.Warning(
                "Can't parse API timeout. Recommended timeout {Recommended}s is used",
                RecommendedTimeout.TotalSeconds);

            return RecommendedTimeout;
        }

        if (timeout == TimeSpan.Zero)
        {
            return Timeout.InfiniteTimeSpan;
        }

        if (timeout >= RecommendedTimeout)
        {
            return timeout;
        }

        if (!isForced)
        {
            StartupLogger.Warning(
                "Timeout {Timeout}s is less than recommended minimum {Recommended}s. Use 'value!' to force",
                timeout.TotalSeconds,
                RecommendedTimeout.TotalSeconds);

            return RecommendedTimeout;
        }

        StartupLogger.Warning(
            "Timeout {Timeout}s is less than recommended minimum {Recommended}s",
            timeout.TotalSeconds,
            RecommendedTimeout.TotalSeconds);

        return timeout;
    }
}
