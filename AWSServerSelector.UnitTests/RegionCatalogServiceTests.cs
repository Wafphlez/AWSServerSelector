using AWSServerSelector.Services;
using Xunit;

namespace AWSServerSelector.UnitTests;

public class RegionCatalogServiceTests
{
    [Fact]
    public void Regions_ContainsKnownRegionsAndHosts()
    {
        var catalog = new RegionCatalogService();

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
        var catalog = new RegionCatalogService();
        Assert.Equal(expected, catalog.GetGroupOrder(group));
    }
}
