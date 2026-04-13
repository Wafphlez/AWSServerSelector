namespace AWSServerSelector.Models;

public sealed record TrayStatusSnapshot(string MatchPing, string Region)
{
    public static TrayStatusSnapshot Unavailable { get; } = new("N/A", "N/A");
}
