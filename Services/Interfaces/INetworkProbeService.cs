using System.Net;
using System.Net.NetworkInformation;

namespace AWSServerSelector.Services.Interfaces;

public interface INetworkProbeService
{
    Task<long> PingAsync(string host, int timeoutMs);
    Task<IPHostEntry> GetHostEntryAsync(string hostOrAddress);
    IPAddress[] ResolveHostAddresses(string host);
}
