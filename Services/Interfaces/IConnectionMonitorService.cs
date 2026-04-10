using System.Threading.Tasks;
using AWSServerSelector.Models;

namespace AWSServerSelector.Services.Interfaces;

public interface IConnectionMonitorService
{
    Task<ConnectionSnapshot> GetCurrentSnapshotAsync();
}
