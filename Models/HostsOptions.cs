namespace AWSServerSelector.Models;

public sealed class HostsOptions
{
    public const string SectionName = "Hosts";
    public string DefaultHostsTemplatePath { get; set; } = "Config/default-hosts.txt";
}
