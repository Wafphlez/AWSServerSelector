using AWSServerSelector.Models;
using AWSServerSelector.Services;
using Microsoft.Extensions.Options;
using Xunit;

namespace AWSServerSelector.UnitTests;

public class RegionCatalogServiceTests
{
    [Fact]
    public void Regions_ContainsKnownRegionsAndHosts()
    {
        var catalog = new RegionCatalogService(CreateOptions());

        Assert.True(catalog.Regions.ContainsKey("Europe (Frankfurt am Main)"));
        Assert.Contains("gamelift.eu-central-1.amazonaws.com", catalog.Regions["Europe (Frankfurt am Main)"].Hosts);
    }

    [Theory]
    [InlineData("Europe", 1)]
    [InlineData("Americas", 2)]
    [InlineData("Asia", 3)]
    [InlineData("Oceania", 4)]
    [InlineData("China", 5)]
    public void GroupOrder_ReturnsExpectedOrder(string group, int expected)
    {
        var catalog = new RegionCatalogService(CreateOptions());
        Assert.Equal(expected, catalog.GetGroupOrder(group));
    }

    private static IOptions<RegionCatalogOptions> CreateOptions()
    {
        return Options.Create(new RegionCatalogOptions
        {
            Regions =
            [
                new("Europe (Frankfurt am Main)", "Europe", "Europe", ["gamelift.eu-central-1.amazonaws.com"], true, "Europe_Frankfurt"),
                new("US East (N. Virginia)", "Americas", "The Americas", ["gamelift.us-east-1.amazonaws.com"], true, "US_East_Virginia"),
                new("Asia Pacific (Tokyo)", "Asia", "Asia (excluding Mainland China)", ["gamelift.ap-northeast-1.amazonaws.com"], true, "Asia_Tokyo"),
                new("Asia Pacific (Sydney)", "Oceania", "Oceania", ["gamelift.ap-southeast-2.amazonaws.com"], true, "Asia_Sydney"),
                new("China (Beijing)", "China", "Mainland China", ["gamelift.cn-north-1.amazonaws.com.cn"], true, "China_Beijing")
            ]
        });
    }
}
