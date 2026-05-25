using System;
using System.Threading;
using MultiFactor.Ldap.Adapter.Configuration;
using Xunit;

namespace MultiFactor.Ldap.Adapter.Tests;

public class ConfigurationValueParserTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ParseTimeout_NullOrWhitespace_ReturnsRecommendedTimeout(string value)
    {
        var timeout = ConfigurationValueParser.ParseTimeout(value);

        Assert.Equal(ConfigurationValueParser.RecommendedTimeout, timeout);
    }

    [Theory]
    [InlineData("not-a-timeout")]
    [InlineData("1:30")]
    [InlineData("abc!")]
    public void ParseTimeout_InvalidFormat_ReturnsRecommendedTimeout(string value)
    {
        var timeout = ConfigurationValueParser.ParseTimeout(value);

        Assert.Equal(ConfigurationValueParser.RecommendedTimeout, timeout);
    }

    [Fact]
    public void ParseTimeout_Zero_ReturnsInfiniteTimeSpan()
    {
        var timeout = ConfigurationValueParser.ParseTimeout("00:00:00");

        Assert.Equal(Timeout.InfiniteTimeSpan, timeout);
    }

    [Theory]
    [InlineData("00:01:05")]
    [InlineData("00:02:00")]
    [InlineData("01:00:00")]
    public void ParseTimeout_AboveOrEqualMinimum_ReturnsConfiguredValue(string value)
    {
        var expected = TimeSpan.ParseExact(value, @"hh\:mm\:ss", null);

        var timeout = ConfigurationValueParser.ParseTimeout(value);

        Assert.Equal(expected, timeout);
    }

    [Theory]
    [InlineData("00:00:30")]
    [InlineData("00:01:04")]
    public void ParseTimeout_BelowMinimumWithoutForce_ReturnsRecommendedTimeout(string value)
    {
        var timeout = ConfigurationValueParser.ParseTimeout(value);

        Assert.Equal(ConfigurationValueParser.RecommendedTimeout, timeout);
    }

    [Theory]
    [InlineData("00:00:01!")]
    [InlineData("00:00:30!")]
    [InlineData("00:01:04!")]
    public void ParseTimeout_BelowMinimumWithForce_ReturnsConfiguredValue(string value)
    {
        var expected = TimeSpan.ParseExact(value[..^1], @"hh\:mm\:ss", null);

        var timeout = ConfigurationValueParser.ParseTimeout(value);

        Assert.Equal(expected, timeout);
    }
}
