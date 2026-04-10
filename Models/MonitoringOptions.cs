namespace AWSServerSelector.Models;

public sealed class MonitoringOptions
{
    public const string SectionName = "Monitoring";
    public int MainPingIntervalSeconds { get; set; }
    public int MainPingTimeoutMs { get; set; }
    public int ConnectionPollIntervalSeconds { get; set; }
    public int ConnectionPingTimeoutMs { get; set; }
    public int ConnectionGamePingIntervalSeconds { get; set; }
    public int IpApiTimeoutSeconds { get; set; }
}
