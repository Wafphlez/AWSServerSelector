using System.ComponentModel;
using System.Windows.Input;
using System.Windows.Media;
using AWSServerSelector.ViewModels;

namespace AWSServerSelector.Models;

public class ServerItem : INotifyPropertyChanged
{
    public string RegionKey { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string ToolTipText { get; set; } = string.Empty;
    public bool IsStable { get; set; }
    public ServerGroupItem? ParentGroup { get; set; }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            _isSelected = value;
            OnPropertyChanged(nameof(IsSelected));
            ParentGroup?.UpdateSelectAllState();
        }
    }

    private string _latencyText = "…";
    public string LatencyText
    {
        get => _latencyText;
        set
        {
            _latencyText = value;
            OnPropertyChanged(nameof(LatencyText));
        }
    }

    private SolidColorBrush _textColor = new(Colors.White);
    public SolidColorBrush TextColor
    {
        get => _textColor;
        set
        {
            _textColor = value;
            OnPropertyChanged(nameof(TextColor));
        }
    }

    private SolidColorBrush _latencyColor = new(Colors.Gray);
    public SolidColorBrush LatencyColor
    {
        get => _latencyColor;
        set
        {
            _latencyColor = value;
            OnPropertyChanged(nameof(LatencyColor));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private ICommand? _toggleSelectionCommand;
    public ICommand ToggleSelectionCommand => _toggleSelectionCommand ??= new RelayCommand(() => IsSelected = !IsSelected);
}
