namespace AWSServerSelector.Services.Interfaces;

public interface IConnectionStatusTextService
{
    string BuildMatchStatusText(string baseStatus, bool npcapWorking, bool npcapUnavailable);
}
