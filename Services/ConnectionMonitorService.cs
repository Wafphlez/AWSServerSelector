using System;
using System.Threading.Tasks;
using AWSServerSelector.Models;
using AWSServerSelector.Services.Interfaces;

namespace AWSServerSelector.Services;

public sealed class ConnectionMonitorService : IConnectionMonitorService
{
    public Task<ConnectionSnapshot> GetCurrentSnapshotAsync()
    {
        var unknown = LocalizationManager.NotDetermined;
        var notConnected = LocalizationManager.NotConnected;
        var notMeasured = LocalizationManager.NotMeasured;

        return Task.FromResult(new ConnectionSnapshot
        {
            LastUpdateText = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"),
            Lobby = new EndpointSnapshot
            {
                StatusText = notConnected,
                IpText = unknown,
                ServerText = unknown,
                RegionText = unknown,
                PingText = notMeasured
            },
            Game = new EndpointSnapshot
            {
                StatusText = notConnected,
                IpText = unknown,
                ServerText = unknown,
                RegionText = unknown,
                PingText = notMeasured
            }
        });
    }
}
