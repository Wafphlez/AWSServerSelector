using AWSServerSelector.Services.Interfaces;

namespace AWSServerSelector.Services;

public sealed class ConnectionStatusTextService : IConnectionStatusTextService
{
    private readonly ILocalizationService _localizationService;

    public ConnectionStatusTextService(ILocalizationService localizationService)
    {
        _localizationService = localizationService;
    }

    public string BuildMatchStatusText(string baseStatus, bool npcapWorking, bool npcapUnavailable)
    {
        var npcapText = npcapWorking
            ? _localizationService.GetString("NpcapStatusOk")
            : npcapUnavailable
                ? _localizationService.GetString("NpcapStatusUnavailable")
                : _localizationService.GetString("NpcapStatusUnknown");
        return $"{baseStatus} ({npcapText})";
    }
}
