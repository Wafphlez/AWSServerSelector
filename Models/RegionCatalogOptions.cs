namespace AWSServerSelector.Models;

public sealed class RegionCatalogOptions
{
    public const string SectionName = "RegionCatalog";
    public List<RegionDefinition> Regions { get; set; } = [];
}
