using AWSServerSelector.Services;
using Xunit;

namespace AWSServerSelector.UnitTests;

public class HostsSectionBuilderTests
{
    private const string Marker = "# -- Ping by Daylight --";

    [Fact]
    public void Build_AppendsSection_WhenNotExists()
    {
        var result = HostsSectionBuilder.Build("127.0.0.1 localhost", Marker, "0.0.0.0 test.local");
        Assert.Contains(Marker, result);
        Assert.Contains("0.0.0.0 test.local", result);
    }

    [Fact]
    public void Build_ReplacesExistingWrappedSection()
    {
        var original = "127.0.0.1 localhost\r\n" +
                       $"{Marker}\r\nold\r\n{Marker}\r\n";
        var result = HostsSectionBuilder.Build(original, Marker, "new");
        Assert.DoesNotContain("\r\nold\r\n", result);
        Assert.Contains("\r\nnew\r\n", result);
    }

    [Fact]
    public void Build_HandlesSingleMarker_AndRewrites()
    {
        var original = "127.0.0.1 localhost\r\n" + Marker + "\r\nlegacy";
        var result = HostsSectionBuilder.Build(original, Marker, "fresh");
        Assert.Contains("fresh", result);
        Assert.Equal(2, CountOccurrences(result, Marker));
    }

    [Fact]
    public void Build_NormalizesTrailingNewlines()
    {
        var result = HostsSectionBuilder.Build(string.Empty, Marker, "line1\r\n\r\n");
        Assert.DoesNotContain("\r\n\r\n\r\n", result);
    }

    private static int CountOccurrences(string value, string token)
    {
        var count = 0;
        var index = 0;
        while ((index = value.IndexOf(token, index, System.StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += token.Length;
        }

        return count;
    }
}
