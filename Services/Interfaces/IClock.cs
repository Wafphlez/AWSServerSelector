namespace AWSServerSelector.Services.Interfaces;

public interface IClock
{
    DateTimeOffset Now { get; }
}
