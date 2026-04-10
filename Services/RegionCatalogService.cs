using AWSServerSelector.Models;
using AWSServerSelector.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace AWSServerSelector.Services;

public sealed class RegionCatalogService : IRegionCatalogService
{
    private readonly Dictionary<string, int> _groupOrders;
    public IReadOnlyDictionary<string, RegionDefinition> Regions { get; }

    public RegionCatalogService(IOptions<RegionCatalogOptions> options)
    {
        var regions = options.Value.Regions;
        if (regions.Count == 0)
        {
            throw new InvalidOperationException("RegionCatalog.Regions must contain at least one region.");
        }

        var duplicateKey = regions
            .GroupBy(region => region.Key, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateKey != null)
        {
            throw new InvalidOperationException($"RegionCatalog.Regions contains duplicate key '{duplicateKey.Key}'.");
        }

        foreach (var region in regions)
        {
            ValidateRegion(region);
        }

        Regions = regions.ToDictionary(region => region.Key, StringComparer.OrdinalIgnoreCase);
        _groupOrders = BuildGroupOrders(regions);
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

    private static void ValidateRegion(RegionDefinition region)
    {
        if (string.IsNullOrWhiteSpace(region.Key))
        {
            throw new InvalidOperationException("RegionCatalog contains a region with empty key.");
        }

        if (string.IsNullOrWhiteSpace(region.GroupKey))
        {
            throw new InvalidOperationException($"Region '{region.Key}' has empty groupKey.");
        }

        if (string.IsNullOrWhiteSpace(region.GroupDisplayName))
        {
            throw new InvalidOperationException($"Region '{region.Key}' has empty groupDisplayName.");
        }

        if (region.Hosts.Length == 0 || region.Hosts.Any(string.IsNullOrWhiteSpace))
        {
            throw new InvalidOperationException($"Region '{region.Key}' must contain non-empty hosts.");
        }

        if (string.IsNullOrWhiteSpace(region.DisplayNameKey))
        {
            throw new InvalidOperationException($"Region '{region.Key}' has empty displayNameKey.");
        }
    }
}
