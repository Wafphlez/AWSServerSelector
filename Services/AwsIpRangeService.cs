using System.Threading.Tasks;
using AWSServerSelector.Services.Interfaces;

namespace AWSServerSelector.Services;

public sealed class AwsIpRangeService : IAwsIpRangeService
{
    public Task<bool> IsAwsIpAsync(string ip) => AwsIpRangeManager.Instance.IsAwsIpAsync(ip);

    public Task<string> GetAwsRegionAsync(string ip) => AwsIpRangeManager.Instance.GetAwsRegionAsync(ip);

    public Task<string> GetAwsServiceAsync(string ip) => AwsIpRangeManager.Instance.GetAwsServiceAsync(ip);
}
