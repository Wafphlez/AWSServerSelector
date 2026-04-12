using AWSServerSelector.Models;
using AWSServerSelector.Services;
using Xunit;

namespace AWSServerSelector.UnitTests;

public class HostsContentBuilderTests
{
    private static readonly IReadOnlyDictionary<string, RegionDefinition> Regions =
        new Dictionary<string, RegionDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            ["eu-central-1"] = new("eu-central-1", "Europe", "Europe", ["svc-eu.example", "ping-eu.example"], true, null),
            ["eu-west-3-unstable"] = new("eu-west-3-unstable", "Europe", "Europe", ["svc-eu-unstable.example"], false, null),
            ["us-east-1"] = new("us-east-1", "Americas", "Americas", ["svc-us.example", "ping-us.example"], true, null)
        };

    [Fact]
    public void BuildUniversalRedirect_UsesServiceAndPingIps()
    {
        var sut = new HostsContentBuilder();

        var result = sut.BuildUniversalRedirect(
            Regions,
            "https://discord.gg/test",
            "1.1.1.1",
            "2.2.2.2",
            key => Regions[key].GroupKey,
            group => group);

        Assert.True(result.Success);
        Assert.Contains("1.1.1.1 svc-eu.example", result.Content);
        Assert.Contains("2.2.2.2 ping-eu.example", result.Content);
        Assert.Contains("1.1.1.1 svc-us.example", result.Content);
    }

    [Fact]
    public void BuildGatekeep_WithMergeUnstable_AddsStableAlternative()
    {
        var sut = new HostsContentBuilder();
        var ordered = new[] { "eu-central-1", "eu-west-3-unstable", "us-east-1" };
        var selected = new[] { "eu-west-3-unstable" };

        var result = sut.BuildGatekeep(
            Regions,
            ordered,
            selected,
            BlockMode.Both,
            mergeUnstable: true,
            "https://discord.gg/test",
            key => Regions[key].GroupKey,
            group => group);

        Assert.True(result.Success);
        Assert.Contains("# svc-eu-unstable.example", result.Content);
        Assert.Contains("# svc-eu.example", result.Content);
        Assert.Contains("0.0.0.0   svc-us.example", result.Content);
    }

    [Fact]
    public void BuildGatekeep_ReturnsError_WhenNoStableInSelectedGroups()
    {
        var regions = new Dictionary<string, RegionDefinition>
        {
            ["custom-unstable"] = new("custom-unstable", "Custom", "Custom", ["svc-custom.example"], false, null)
        };

        var sut = new HostsContentBuilder();

        var result = sut.BuildGatekeep(
            regions,
            ["custom-unstable"],
            ["custom-unstable"],
            BlockMode.Both,
            mergeUnstable: true,
            "https://discord.gg/test",
            key => regions[key].GroupKey,
            group => group);

        Assert.False(result.Success);
        Assert.Contains("стабильные серверы не найдены", result.ErrorMessage!, StringComparison.OrdinalIgnoreCase);
    }
}
