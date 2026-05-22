using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using MultiFactor.Ldap.Adapter.Core.Logging;

namespace MultiFactor.Ldap.Adapter.Configuration;

public static class ConfigurationValueParser
{
    public static TimeSpan RecommendedTimeout { get; } = TimeSpan.FromSeconds(65);

    public static bool TryParseTimeout(string value, [NotNullWhen(true)] out TimeSpan? result)
    {
        result = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
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

            return false;
        }

        if (timeout == TimeSpan.Zero)
        {
            result = Timeout.InfiniteTimeSpan;

            return true;
        }

        if (timeout >= RecommendedTimeout)
        {
            result = timeout;

            return true;
        }

        if (!isForced)
        {
            StartupLogger.Warning(
                "Timeout {Timeout}s is less than recommended minimum {Recommended}s. Use 'value!' to force",
                timeout.TotalSeconds,
                RecommendedTimeout.TotalSeconds);

            result = RecommendedTimeout;
        }
        else
        {
            StartupLogger.Warning(
                "Timeout {Timeout}s is less than recommended minimum {Recommended}s",
                timeout.TotalSeconds,
                RecommendedTimeout.TotalSeconds);

            result = timeout;
        }

        return true;
    }
}