using System.Text;
using AWSServerSelector.Models;
using AWSServerSelector.Services.Interfaces;

namespace AWSServerSelector.Services;

public sealed class HostsContentBuilder : IHostsContentBuilder
{
    public HostsContentBuildResult BuildUniversalRedirect(
        IReadOnlyDictionary<string, RegionDefinition> regions,
        string discordUrl,
        string serviceIp,
        string pingIp,
        Func<string, string> getGroupName,
        Func<string, string> getGroupDisplayName)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Edited by Ping by Daylight");
        sb.AppendLine("# Universal Redirect mode: redirect all GameLift endpoints to selected region");
        sb.AppendLine($"# Need help? Discord: {discordUrl}");
        sb.AppendLine();

        string currentGroup = string.Empty;
        foreach (var kv in regions)
        {
            var groupName = getGroupName(kv.Key);
            if (groupName != currentGroup)
            {
                sb.AppendLine($"# {getGroupDisplayName(groupName)}");
                currentGroup = groupName;
            }

            foreach (var host in kv.Value.Hosts)
            {
                var isPing = host.Contains("ping", StringComparison.OrdinalIgnoreCase);
                sb.AppendLine($"{(isPing ? pingIp : serviceIp)} {host}");
            }
            sb.AppendLine();
        }

        return new HostsContentBuildResult
        {
            Success = true,
            Content = sb.ToString()
        };
    }

    public HostsContentBuildResult BuildGatekeep(
        IReadOnlyDictionary<string, RegionDefinition> regions,
        IReadOnlyCollection<string> orderedRegionKeys,
        IReadOnlyCollection<string> selectedRegions,
        BlockMode blockMode,
        bool mergeUnstable,
        string discordUrl,
        Func<string, string> getGroupName,
        Func<string, string> getGroupDisplayName)
    {
        var allowedSet = new HashSet<string>(selectedRegions);
        var anyStableSelected = selectedRegions.Any(regionKey =>
            regions.TryGetValue(regionKey, out var region) && region.Stable);

        if (mergeUnstable && !anyStableSelected)
        {
            var missing = new List<string>();
            foreach (var regionKey in selectedRegions)
            {
                if (!regions.TryGetValue(regionKey, out var region) || region.Stable)
                {
                    continue;
                }

                var group = getGroupName(regionKey);
                var stableExists = regions.Any(kv =>
                    getGroupName(kv.Key) == group && kv.Value.Stable);
                if (!stableExists)
                {
                    missing.Add(regionKey);
                }
            }

            if (missing.Count > 0)
            {
                return new HostsContentBuildResult
                {
                    Success = false,
                    ErrorMessage = "Опция объединения нестабильных серверов включена, но стабильные серверы не найдены для: " +
                                   string.Join(", ", missing) +
                                   ".\nОтключите объединение нестабильных серверов в меню настроек или выберите стабильный сервер вручную."
                };
            }
        }

        if (mergeUnstable && !anyStableSelected)
        {
            var additional = new List<string>();
            foreach (var regionKey in allowedSet.ToList())
            {
                if (!regions.TryGetValue(regionKey, out var region) || region.Stable)
                {
                    continue;
                }

                var group = getGroupName(regionKey);
                var alternative = regions.FirstOrDefault(kv =>
                    getGroupName(kv.Key) == group && kv.Value.Stable);
                if (!string.IsNullOrEmpty(alternative.Key))
                {
                    additional.Add(alternative.Key);
                }
            }

            foreach (var extra in additional)
            {
                allowedSet.Add(extra);
            }
        }

        var sb = new StringBuilder();
        sb.AppendLine("# Edited by Ping by Daylight");
        sb.AppendLine("# Unselected servers are blocked (Gatekeep Mode); selected servers are commented out.");
        sb.AppendLine($"# Need help? Discord: {discordUrl}");
        sb.AppendLine();

        string currentGroup = string.Empty;
        foreach (var regionKey in orderedRegionKeys)
        {
            if (!regions.TryGetValue(regionKey, out var region))
            {
                continue;
            }

            var groupName = getGroupName(regionKey);
            if (groupName != currentGroup)
            {
                sb.AppendLine($"# {getGroupDisplayName(groupName)}");
                currentGroup = groupName;
            }

            var allow = allowedSet.Contains(regionKey);
            foreach (var host in region.Hosts)
            {
                var isPing = host.Contains("ping", StringComparison.OrdinalIgnoreCase);
                var include = blockMode == BlockMode.Both
                              || (blockMode == BlockMode.OnlyPing && isPing)
                              || (blockMode == BlockMode.OnlyService && !isPing);
                if (!include)
                {
                    continue;
                }

                var prefix = allow ? "#" : "0.0.0.0".PadRight(9);
                sb.AppendLine($"{prefix} {host}");
            }
            sb.AppendLine();
        }

        return new HostsContentBuildResult
        {
            Success = true,
            Content = sb.ToString()
        };
    }
}
