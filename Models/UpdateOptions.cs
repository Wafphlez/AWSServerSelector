namespace AWSServerSelector.Models;

public sealed class UpdateOptions
{
    public const string SectionName = "Update";
    public string LatestReleaseApiUrl { get; set; } = "https://api.github.com/repos/Wafphlez/AWSServerSelector/releases/latest";
    public string UserAgent { get; set; } = "AWSServerSelector-UpdateChecker/1.0";
    public int TimeoutSeconds { get; set; } = 15;
}
