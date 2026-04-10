using AWSServerSelector.Models;

namespace AWSServerSelector.Services.Interfaces;

public interface ISettingsService
{
    UserSettings Load();
    void Save(UserSettings settings);
}
