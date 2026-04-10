namespace AWSServerSelector.Models;

public sealed class MonitoringOptions
{
    public const string SectionName = "Monitoring";
    public int MainPingIntervalSeconds { get; set; } = 5;
    public int MainPingTimeoutMs { get; set; } = 2000;
    public int ConnectionPollIntervalSeconds { get; set; } = 5;
    public int ConnectionPingTimeoutMs { get; set; } = 2000;
    public int ConnectionGamePingIntervalSeconds { get; set; } = 1;
    public int IpApiTimeoutSeconds { get; set; } = 5;
}
