using AWSServerSelector.Services.Interfaces;

namespace AWSServerSelector.Services;

public sealed class LocalizationService : ILocalizationService
{
    public event EventHandler? LanguageChanged
    {
        add => LocalizationManager.LanguageChanged += value;
        remove => LocalizationManager.LanguageChanged -= value;
    }

    public string GetString(string key) => LocalizationManager.GetString(key);

    public string GetString(string key, params object[] args) => LocalizationManager.GetString(key, args);

    public string GetServerDisplayName(string regionKey, string? displayNameKey = null) =>
        LocalizationManager.GetServerDisplayName(regionKey, displayNameKey);

    public string GetGroupDisplayName(string groupName) =>
        LocalizationManager.GetGroupDisplayName(groupName);

    public void SetLanguage(string languageCode) =>
        LocalizationManager.SetLanguage(languageCode);

    public void SetLanguageAndNotify(string languageCode) =>
        LocalizationManager.SetLanguageAndNotify(languageCode);
}
