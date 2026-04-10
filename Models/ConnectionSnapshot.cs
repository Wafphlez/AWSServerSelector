namespace AWSServerSelector.Models;

public sealed class EndpointSnapshot
{
    public string StatusText { get; set; } = string.Empty;
    public string IpText { get; set; } = string.Empty;
    public string ServerText { get; set; } = string.Empty;
    public string RegionText { get; set; } = string.Empty;
    public string PingText { get; set; } = string.Empty;
}

public sealed class ConnectionSnapshot
{
    public EndpointSnapshot Lobby { get; set; } = new();
    public EndpointSnapshot Game { get; set; } = new();
    public string LastUpdateText { get; set; } = string.Empty;
}
