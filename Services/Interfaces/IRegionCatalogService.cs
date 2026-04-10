using AWSServerSelector.Models;

namespace AWSServerSelector.Services.Interfaces;

public interface IRegionCatalogService
{
    IReadOnlyDictionary<string, RegionDefinition> Regions { get; }
    string GetGroupName(string regionKey);
    string GetGroupDisplayName(string groupName);
    int GetGroupOrder(string groupName);
}
