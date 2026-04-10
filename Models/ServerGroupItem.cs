using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace AWSServerSelector.Models;

public class ServerGroupItem : INotifyPropertyChanged
{
    public string GroupName { get; set; } = string.Empty;
    public ObservableCollection<ServerItem> Servers { get; set; } = new();
    public bool IsExpanded { get; set; } = true;

    private bool _isGroupHovered;
    public bool IsGroupHovered
    {
        get => _isGroupHovered;
        set
        {
            if (_isGroupHovered == value) return;
            _isGroupHovered = value;
            OnPropertyChanged(nameof(IsGroupHovered));
        }
    }

    private bool? _isAllSelected;
    public bool? IsAllSelected
    {
        get => _isAllSelected;
        set
        {
            if (_isAllSelected == value) return;
            _isAllSelected = value;
            OnPropertyChanged(nameof(IsAllSelected));

            if (!value.HasValue) return;
            foreach (var server in Servers)
            {
                var oldParent = server.ParentGroup;
                server.ParentGroup = null;
                server.IsSelected = value.Value;
                server.ParentGroup = oldParent;
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void UpdateSelectAllState()
    {
        if (Servers.Count == 0)
        {
            _isAllSelected = false;
            OnPropertyChanged(nameof(IsAllSelected));
            return;
        }

        var selectedCount = Servers.Count(s => s.IsSelected);
        bool? newState = selectedCount switch
        {
            0 => false,
            _ when selectedCount == Servers.Count => true,
            _ => null
        };

        if (_isAllSelected == newState) return;
        _isAllSelected = newState;
        OnPropertyChanged(nameof(IsAllSelected));
    }
}
