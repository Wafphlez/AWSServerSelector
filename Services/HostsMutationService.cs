using AWSServerSelector.Services.Interfaces;

namespace AWSServerSelector.Services;

public sealed class HostsMutationService : IHostsMutationService
{
    private readonly IHostsService _hostsService;

    public HostsMutationService(IHostsService hostsService)
    {
        _hostsService = hostsService;
    }

    public string Read() => _hostsService.Read();

    public void Write(string content) => _hostsService.Write(content);

    public void Backup() => _hostsService.Backup();

    public void FlushDns() => _hostsService.FlushDns();

    public bool IsHostBlocked(string host) => _hostsService.IsHostBlocked(host);

    public string ReadDefaultTemplate() => _hostsService.ReadDefaultTemplate();
}
