using AWSServerSelector.Models;
using AWSServerSelector.Services.Interfaces;

namespace AWSServerSelector.Services;

public sealed class RegionCatalogService : IRegionCatalogService
{
    public IReadOnlyDictionary<string, RegionDefinition> Regions { get; } = new Dictionary<string, RegionDefinition>
    {
        ["Europe (London)"] = new("Europe (London)", "Europe", "Europe", ["gamelift.eu-west-2.amazonaws.com", "gamelift-ping.eu-west-2.api.aws"], false),
        ["Europe (Ireland)"] = new("Europe (Ireland)", "Europe", "Europe", ["gamelift.eu-west-1.amazonaws.com", "gamelift-ping.eu-west-1.api.aws"], true),
        ["Europe (Frankfurt am Main)"] = new("Europe (Frankfurt am Main)", "Europe", "Europe", ["gamelift.eu-central-1.amazonaws.com", "gamelift-ping.eu-central-1.api.aws"], true),
        ["US East (N. Virginia)"] = new("US East (N. Virginia)", "Americas", "The Americas", ["gamelift.us-east-1.amazonaws.com", "gamelift-ping.us-east-1.api.aws"], true),
        ["US East (Ohio)"] = new("US East (Ohio)", "Americas", "The Americas", ["gamelift.us-east-2.amazonaws.com", "gamelift-ping.us-east-2.api.aws"], false),
        ["US West (N. California)"] = new("US West (N. California)", "Americas", "The Americas", ["gamelift.us-west-1.amazonaws.com", "gamelift-ping.us-west-1.api.aws"], true),
        ["US West (Oregon)"] = new("US West (Oregon)", "Americas", "The Americas", ["gamelift.us-west-2.amazonaws.com", "gamelift-ping.us-west-2.api.aws"], true),
        ["Canada (Central)"] = new("Canada (Central)", "Americas", "The Americas", ["gamelift.ca-central-1.amazonaws.com", "gamelift-ping.ca-central-1.api.aws"], false),
        ["South America (São Paulo)"] = new("South America (São Paulo)", "Americas", "The Americas", ["gamelift.sa-east-1.amazonaws.com", "gamelift-ping.sa-east-1.api.aws"], true),
        ["Asia Pacific (Tokyo)"] = new("Asia Pacific (Tokyo)", "Asia", "Asia (excluding Mainland China)", ["gamelift.ap-northeast-1.amazonaws.com", "gamelift-ping.ap-northeast-1.api.aws"], true),
        ["Asia Pacific (Seoul)"] = new("Asia Pacific (Seoul)", "Asia", "Asia (excluding Mainland China)", ["gamelift.ap-northeast-2.amazonaws.com", "gamelift-ping.ap-northeast-2.api.aws"], true),
        ["Asia Pacific (Mumbai)"] = new("Asia Pacific (Mumbai)", "Asia", "Asia (excluding Mainland China)", ["gamelift.ap-south-1.amazonaws.com", "gamelift-ping.ap-south-1.api.aws"], true),
        ["Asia Pacific (Singapore)"] = new("Asia Pacific (Singapore)", "Asia", "Asia (excluding Mainland China)", ["gamelift.ap-southeast-1.amazonaws.com", "gamelift-ping.ap-southeast-1.api.aws"], true),
        ["Asia Pacific (Hong Kong)"] = new("Asia Pacific (Hong Kong)", "Asia", "Asia (excluding Mainland China)", ["ec2.ap-east-1.amazonaws.com", "gamelift-ping.ap-east-1.api.aws"], true),
        ["Asia Pacific (Sydney)"] = new("Asia Pacific (Sydney)", "Oceania", "Oceania", ["gamelift.ap-southeast-2.amazonaws.com", "gamelift-ping.ap-southeast-2.api.aws"], true),
        ["China (Beijing)"] = new("China (Beijing)", "China", "Mainland China", ["gamelift.cn-north-1.amazonaws.com.cn"], true),
        ["China (Ningxia)"] = new("China (Ningxia)", "China", "Mainland China", ["gamelift.cn-northwest-1.amazonaws.com.cn"], true)
    };

    public string GetGroupName(string regionKey) => Regions.TryGetValue(regionKey, out var region) ? region.GroupKey : "Other";

    public string GetGroupDisplayName(string groupName) => groupName switch
    {
        "Europe" => "Europe",
        "Americas" => "The Americas",
        "Asia" => "Asia (excluding Mainland China)",
        "Oceania" => "Oceania",
        "China" => "Mainland China",
        _ => groupName
    };

    public int GetGroupOrder(string groupName) => groupName switch
    {
        "Europe" => 1,
        "Americas" => 2,
        "Asia" => 3,
        "Oceania" => 4,
        "China" => 5,
        _ => 6
    };
}
