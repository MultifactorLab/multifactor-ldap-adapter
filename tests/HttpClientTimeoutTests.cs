using System;
using System.Threading;
using MultiFactor.Ldap.Adapter.Configuration;
using Xunit;

namespace MultiFactor.Ldap.Adapter.Tests;

public class HttpClientTimeoutTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_NullOrWhitespace_ReturnsRecommended(string value)
    {
        var timeout = HttpClientTimeout.Parse(value);

        Assert.Equal(HttpClientTimeout.Recommended, timeout.Value);
        Assert.Null(timeout.Warning);
    }

    [Theory]
    [InlineData("not-a-timeout")]
    [InlineData("1:30")]
    [InlineData("abc!")]
    public void Parse_InvalidFormat_ReturnsRecommendedWithWarning(string value)
    {
        var timeout = HttpClientTimeout.Parse(value);

        Assert.Equal(HttpClientTimeout.Recommended, timeout.Value);
        Assert.NotNull(timeout.Warning);
    }

    [Fact]
    public void Parse_Zero_ReturnsInfiniteTimeSpan()
    {
        var timeout = HttpClientTimeout.Parse("00:00:00");

        Assert.Equal(Timeout.InfiniteTimeSpan, timeout.Value);
        Assert.Null(timeout.Warning);
    }

    [Theory]
    [InlineData("00:01:05")]
    [InlineData("00:02:00")]
    [InlineData("01:00:00")]
    public void Parse_AboveOrEqualMinimum_ReturnsConfiguredValue(string value)
    {
        var expected = TimeSpan.ParseExact(value, @"hh\:mm\:ss", null);

        var timeout = HttpClientTimeout.Parse(value);

        Assert.Equal(expected, timeout.Value);
        Assert.Null(timeout.Warning);
    }

    [Theory]
    [InlineData("00:00:30")]
    [InlineData("00:01:04")]
    public void Parse_BelowMinimumWithoutForce_ReturnsRecommendedWithWarning(string value)
    {
        var timeout = HttpClientTimeout.Parse(value);

        Assert.Equal(HttpClientTimeout.Recommended, timeout.Value);
        Assert.Contains("Use 'value!' to force", timeout.Warning);
    }

    [Theory]
    [InlineData("00:00:01!")]
    [InlineData("00:00:30!")]
    [InlineData("00:01:04!")]
    public void Parse_BelowMinimumWithForce_ReturnsConfiguredValueWithWarning(string value)
    {
        var expected = TimeSpan.ParseExact(value.TrimEnd('!'), @"hh\:mm\:ss", null);

        var timeout = HttpClientTimeout.Parse(value);

        Assert.Equal(expected, timeout.Value);
        Assert.NotNull(timeout.Warning);
        Assert.DoesNotContain("Use 'value!' to force", timeout.Warning);
    }
}
