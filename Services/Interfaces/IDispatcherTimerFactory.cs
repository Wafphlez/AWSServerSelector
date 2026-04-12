using System.Windows.Threading;

namespace AWSServerSelector.Services.Interfaces;

public interface IDispatcherTimerFactory
{
    DispatcherTimer Create(TimeSpan interval);
}
