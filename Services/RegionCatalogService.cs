using AWSServerSelector.Models;
using AWSServerSelector.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace AWSServerSelector.Services;

public sealed class RegionCatalogService : IRegionCatalogService
{
    private readonly Dictionary<string, int> _groupOrders;
    public IReadOnlyDictionary<string, RegionDefinition> Regions { get; }

    public RegionCatalogService(IOptions<RegionCatalogOptions>? options = null)
    {
        var configuredRegions = options?.Value?.Regions ?? [];
        var sanitized = configuredRegions
            .Where(region => !string.IsNullOrWhiteSpace(region.Key) && region.Hosts.Length > 0)
            .GroupBy(region => region.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();
        var source = sanitized.Count > 0 ? sanitized : GetFallbackRegions();

        if (sanitized.Count == 0)
        {
            AppLogger.Error("Region catalog config is empty or missing. Using built-in fallback.");
        }

        Regions = source.ToDictionary(region => region.Key, StringComparer.OrdinalIgnoreCase);
        _groupOrders = BuildGroupOrders(source);
    }

    public string GetGroupName(string regionKey) => Regions.TryGetValue(regionKey, out var region) ? region.GroupKey : "Other";

    public string GetGroupDisplayName(string groupName)
    {
        var matched = Regions.Values.FirstOrDefault(
            region => string.Equals(region.GroupKey, groupName, StringComparison.OrdinalIgnoreCase));
        return matched?.GroupDisplayName ?? groupName;
    }

    public int GetGroupOrder(string groupName) => _groupOrders.TryGetValue(groupName, out var order) ? order : int.MaxValue;

    private static Dictionary<string, int> BuildGroupOrders(IEnumerable<RegionDefinition> regions)
    {
        var orders = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var current = 1;
        foreach (var region in regions)
        {
            if (!orders.ContainsKey(region.GroupKey))
            {
                orders[region.GroupKey] = current++;
            }
        }

        return orders;
    }

    private static List<RegionDefinition> GetFallbackRegions() =>
    [
        new("Europe (London)", "Europe", "Europe", ["gamelift.eu-west-2.amazonaws.com", "gamelift-ping.eu-west-2.api.aws"], false, "Europe_London"),
        new("Europe (Ireland)", "Europe", "Europe", ["gamelift.eu-west-1.amazonaws.com", "gamelift-ping.eu-west-1.api.aws"], true, "Europe_Ireland"),
        new("Europe (Frankfurt am Main)", "Europe", "Europe", ["gamelift.eu-central-1.amazonaws.com", "gamelift-ping.eu-central-1.api.aws"], true, "Europe_Frankfurt"),
        new("US East (N. Virginia)", "Americas", "The Americas", ["gamelift.us-east-1.amazonaws.com", "gamelift-ping.us-east-1.api.aws"], true, "US_East_Virginia"),
        new("US East (Ohio)", "Americas", "The Americas", ["gamelift.us-east-2.amazonaws.com", "gamelift-ping.us-east-2.api.aws"], false, "US_East_Ohio"),
        new("US West (N. California)", "Americas", "The Americas", ["gamelift.us-west-1.amazonaws.com", "gamelift-ping.us-west-1.api.aws"], true, "US_West_California"),
        new("US West (Oregon)", "Americas", "The Americas", ["gamelift.us-west-2.amazonaws.com", "gamelift-ping.us-west-2.api.aws"], true, "US_West_Oregon"),
        new("Canada (Central)", "Americas", "The Americas", ["gamelift.ca-central-1.amazonaws.com", "gamelift-ping.ca-central-1.api.aws"], false, "Canada_Central"),
        new("South America (São Paulo)", "Americas", "The Americas", ["gamelift.sa-east-1.amazonaws.com", "gamelift-ping.sa-east-1.api.aws"], true, "South_America_Sao_Paulo"),
        new("Asia Pacific (Tokyo)", "Asia", "Asia (excluding Mainland China)", ["gamelift.ap-northeast-1.amazonaws.com", "gamelift-ping.ap-northeast-1.api.aws"], true, "Asia_Tokyo"),
        new("Asia Pacific (Seoul)", "Asia", "Asia (excluding Mainland China)", ["gamelift.ap-northeast-2.amazonaws.com", "gamelift-ping.ap-northeast-2.api.aws"], true, "Asia_Seoul"),
        new("Asia Pacific (Mumbai)", "Asia", "Asia (excluding Mainland China)", ["gamelift.ap-south-1.amazonaws.com", "gamelift-ping.ap-south-1.api.aws"], true, "Asia_Mumbai"),
        new("Asia Pacific (Singapore)", "Asia", "Asia (excluding Mainland China)", ["gamelift.ap-southeast-1.amazonaws.com", "gamelift-ping.ap-southeast-1.api.aws"], true, "Asia_Singapore"),
        new("Asia Pacific (Hong Kong)", "Asia", "Asia (excluding Mainland China)", ["ec2.ap-east-1.amazonaws.com", "gamelift-ping.ap-east-1.api.aws"], true, "Asia_Hong_Kong"),
        new("Asia Pacific (Sydney)", "Oceania", "Oceania", ["gamelift.ap-southeast-2.amazonaws.com", "gamelift-ping.ap-southeast-2.api.aws"], true, "Asia_Sydney"),
        new("China (Beijing)", "China", "Mainland China", ["gamelift.cn-north-1.amazonaws.com.cn"], true, "China_Beijing"),
        new("China (Ningxia)", "China", "Mainland China", ["gamelift.cn-northwest-1.amazonaws.com.cn"], true, "China_Ningxia")
    ];
}
