namespace AWSServerSelector.Models;

public sealed class UserSettings
{
    public ApplyMode ApplyMode { get; set; }
    public BlockMode BlockMode { get; set; }
    public bool MergeUnstable { get; set; } = true;
    public string Language { get; set; } = "en";
}
