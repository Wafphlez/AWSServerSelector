using System.Net.NetworkInformation;
using System.Threading.Tasks;
using AWSServerSelector.Services.Interfaces;

namespace AWSServerSelector.Services;

public sealed class LatencyService : ILatencyService
{
    public async Task<long> PingAsync(string host, int timeoutMs = 2000)
    {
        using var pinger = new Ping();
        var reply = await pinger.SendPingAsync(host, timeoutMs);
        return reply.Status == IPStatus.Success ? reply.RoundtripTime : -1;
    }
}
