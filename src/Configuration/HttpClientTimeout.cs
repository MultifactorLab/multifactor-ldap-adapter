using System;
using System.Threading;

namespace MultiFactor.Ldap.Adapter.Configuration;

public sealed class HttpClientTimeout
{
    public static readonly TimeSpan Recommended = TimeSpan.FromSeconds(65);

    public TimeSpan Value { get; }

    public string Warning { get; }

    private HttpClientTimeout(TimeSpan value, string warning = null)
    {
        Value = value;
        Warning = warning;
    }

    public static HttpClientTimeout Parse(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return new HttpClientTimeout(Recommended);
        }

        var isForced = raw.EndsWith('!');
        if (isForced)
        {
            raw = raw.TrimEnd('!');
        }

        if (!TimeSpan.TryParseExact(raw, @"hh\:mm\:ss", null,
                System.Globalization.TimeSpanStyles.None, out var result))
        {
            return new HttpClientTimeout(Recommended,
                $"Can't parse API timeout. Recommended timeout {Recommended} is used");
        }

        if (result == TimeSpan.Zero)
        {
            return new HttpClientTimeout(Timeout.InfiniteTimeSpan);
        }

        if (result >= Recommended)
        {
            return new HttpClientTimeout(result);
        }

        if (!isForced)
        {
            return new HttpClientTimeout(Recommended,
                $"Timeout {result} is less than recommended minimum {Recommended}. Use 'value!' to force");
        }

        return new HttpClientTimeout(result,
            $"Timeout {result} is less than recommended minimum {Recommended}");
    }

    public static implicit operator TimeSpan(HttpClientTimeout t) => t.Value;
}
