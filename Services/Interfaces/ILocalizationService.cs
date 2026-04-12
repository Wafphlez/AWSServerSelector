namespace AWSServerSelector.Services.Interfaces;

public interface ILocalizationService
{
    event EventHandler? LanguageChanged;
    string GetString(string key);
    string GetString(string key, params object[] args);
    string GetServerDisplayName(string regionKey, string? displayNameKey = null);
    string GetGroupDisplayName(string groupName);
    void SetLanguage(string languageCode);
    void SetLanguageAndNotify(string languageCode);
}
