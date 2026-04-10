namespace AWSServerSelector.Services.Interfaces;

public interface IDialogService
{
    bool? ShowSettingsDialog(SettingsDialog dialog);
    bool? ShowUpdateDialog(UpdateDialog dialog);
    bool? ShowAboutDialog(AboutDialog dialog);
}
