using AWSServerSelector.Services;
using AWSServerSelector.Services.Interfaces;
using Xunit;

namespace AWSServerSelector.UnitTests;

public class ConnectionStatusTextServiceTests
{
    private readonly ConnectionStatusTextService _sut = new(new FakeLocalizationService());

    [Fact]
    public void BuildMatchStatusText_UsesOkState()
    {
        var text = _sut.BuildMatchStatusText("Connected", npcapWorking: true, npcapUnavailable: false);

        Assert.Contains("Connected", text);
        Assert.Contains("Npcap", text);
    }

    [Fact]
    public void BuildMatchStatusText_UsesUnavailableState()
    {
        var text = _sut.BuildMatchStatusText("Not Connected", npcapWorking: false, npcapUnavailable: true);

        Assert.Contains("Not Connected", text);
        Assert.Contains("Npcap", text);
    }

    private sealed class FakeLocalizationService : ILocalizationService
    {
        public event EventHandler? LanguageChanged;

        public string GetString(string key) => key;

        public string GetString(string key, params object[] args) => key;

        public string GetServerDisplayName(string regionKey, string? displayNameKey = null) => regionKey;

        public string GetGroupDisplayName(string groupName) => groupName;

        public void SetLanguage(string languageCode) { }

        public void SetLanguageAndNotify(string languageCode) => LanguageChanged?.Invoke(this, EventArgs.Empty);
    }
}
