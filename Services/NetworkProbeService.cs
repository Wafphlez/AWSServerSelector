using System.Net;
using System.Net.NetworkInformation;
using AWSServerSelector.Services.Interfaces;

namespace AWSServerSelector.Services;

public sealed class NetworkProbeService : INetworkProbeService
{
    public async Task<long> PingAsync(string host, int timeoutMs)
    {
        using var pinger = new Ping();
        var reply = await pinger.SendPingAsync(host, timeoutMs);
        return reply.Status == IPStatus.Success ? reply.RoundtripTime : -1;
    }

    public Task<IPHostEntry> GetHostEntryAsync(string hostOrAddress)
    {
        return Dns.GetHostEntryAsync(hostOrAddress);
    }

    public IPAddress[] ResolveHostAddresses(string host)
    {
        return Dns.GetHostAddresses(host);
    }
}
