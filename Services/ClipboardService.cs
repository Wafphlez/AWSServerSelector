using System.Windows;
using AWSServerSelector.Services.Interfaces;

namespace AWSServerSelector.Services;

public sealed class ClipboardService : IClipboardService
{
    public void SetText(string text)
    {
        Clipboard.SetText(text);
    }
}
