using System.Threading.Tasks;

namespace AWSServerSelector.Services.Interfaces;

public interface ILatencyService
{
    Task<long> PingAsync(string host, int timeoutMs = 2000);
}
