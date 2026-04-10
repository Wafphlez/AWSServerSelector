namespace AWSServerSelector.Models;

public sealed record RegionDefinition(
    string Key,
    string GroupKey,
    string GroupDisplayName,
    string[] Hosts,
    bool Stable);
