using System.Collections.ObjectModel;
using AWSServerSelector.Models;
using AWSServerSelector.Services.Interfaces;
using AWSServerSelector.ViewModels;
using Xunit;

namespace AWSServerSelector.UnitTests;

public class MainWindowViewModelLocalizationTests
{
    [Fact]
    public void NotifyLocalizationChanged_RefreshesLocalizedProperties()
    {
        var localization = new FakeLocalizationService();
        var vm = new MainWindowViewModel(
            new ObservableCollection<ServerItem>(),
            new ObservableCollection<ServerGroupItem>(),
            () => { }, () => { }, () => { }, () => { }, () => { }, () => { }, () => { },
            localization);

        localization.Values["AppTitle"] = "Title RU";
        localization.Values["Settings"] = "Настройки";
        localization.Values["ApplySelection"] = "Применить";

        vm.NotifyLocalizationChanged();

        Assert.Equal("Title RU", vm.WindowTitle);
        Assert.Equal("Настройки", vm.SettingsMenuText);
        Assert.Equal("Применить", vm.ApplyButtonText);
    }

    private sealed class FakeLocalizationService : ILocalizationService
    {
        public Dictionary<string, string> Values { get; } = new(StringComparer.OrdinalIgnoreCase);

        public event EventHandler? LanguageChanged;

        public string GetString(string key) => Values.TryGetValue(key, out var value) ? value : key;

        public string GetString(string key, params object[] args) => GetString(key);

        public string GetServerDisplayName(string regionKey, string? displayNameKey = null) =>
            displayNameKey is not null && Values.TryGetValue(displayNameKey, out var value) ? value : regionKey;

        public string GetGroupDisplayName(string groupName) => GetString(groupName);

        public void SetLanguage(string languageCode) { }

        public void SetLanguageAndNotify(string languageCode) => LanguageChanged?.Invoke(this, EventArgs.Empty);
    }
}
