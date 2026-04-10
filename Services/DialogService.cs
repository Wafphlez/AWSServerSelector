using System.Windows;
using AWSServerSelector.Services.Interfaces;

namespace AWSServerSelector.Services;

public sealed class DialogService : IDialogService
{
    public bool? ShowSettingsDialog(SettingsDialog dialog) => dialog.ShowDialog();

    public bool? ShowUpdateDialog(UpdateDialog dialog) => dialog.ShowDialog();

    public bool? ShowAboutDialog(AboutDialog dialog) => dialog.ShowDialog();
}
