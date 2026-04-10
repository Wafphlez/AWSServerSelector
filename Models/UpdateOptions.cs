namespace AWSServerSelector.Models;

public sealed class UpdateOptions
{
    public const string SectionName = "Update";
    public string LatestReleaseApiUrl { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; }
}
