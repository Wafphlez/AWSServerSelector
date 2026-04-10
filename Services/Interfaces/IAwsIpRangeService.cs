using System.Threading.Tasks;

namespace AWSServerSelector.Services.Interfaces;

public interface IAwsIpRangeService
{
    Task<bool> IsAwsIpAsync(string ip);
    Task<string> GetAwsRegionAsync(string ip);
    Task<string> GetAwsServiceAsync(string ip);
}
