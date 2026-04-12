using AWSServerSelector.Services.Interfaces;

namespace AWSServerSelector.Services;

public sealed class SystemClock : IClock
{
    public DateTimeOffset Now => DateTimeOffset.Now;
}
