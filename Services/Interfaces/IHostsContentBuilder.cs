using AWSServerSelector.Models;

namespace AWSServerSelector.Services.Interfaces;

public interface IHostsContentBuilder
{
    HostsContentBuildResult BuildUniversalRedirect(
        IReadOnlyDictionary<string, RegionDefinition> regions,
        string discordUrl,
        string serviceIp,
        string pingIp,
        Func<string, string> getGroupName,
        Func<string, string> getGroupDisplayName);

    HostsContentBuildResult BuildGatekeep(
        IReadOnlyDictionary<string, RegionDefinition> regions,
        IReadOnlyCollection<string> orderedRegionKeys,
        IReadOnlyCollection<string> selectedRegions,
        BlockMode blockMode,
        bool mergeUnstable,
        string discordUrl,
        Func<string, string> getGroupName,
        Func<string, string> getGroupDisplayName);
}

public sealed class HostsContentBuildResult
{
    public bool Success { get; init; }
    public string Content { get; init; } = string.Empty;
    public string? ErrorMessage { get; init; }
}
