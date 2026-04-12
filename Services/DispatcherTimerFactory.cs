using System.Windows.Threading;
using AWSServerSelector.Services.Interfaces;

namespace AWSServerSelector.Services;

public sealed class DispatcherTimerFactory : IDispatcherTimerFactory
{
    public DispatcherTimer Create(TimeSpan interval)
    {
        return new DispatcherTimer { Interval = interval };
    }
}
