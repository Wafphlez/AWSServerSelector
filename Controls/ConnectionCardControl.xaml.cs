using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace AWSServerSelector.Controls;

public partial class ConnectionCardControl : UserControl
{
    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(nameof(Icon), typeof(string), typeof(ConnectionCardControl), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty HeaderTextProperty =
        DependencyProperty.Register(nameof(HeaderText), typeof(string), typeof(ConnectionCardControl), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty StatusLabelProperty =
        DependencyProperty.Register(nameof(StatusLabel), typeof(string), typeof(ConnectionCardControl), new PropertyMetadata(string.Empty));
    public static readonly DependencyProperty StatusTextProperty =
        DependencyProperty.Register(nameof(StatusText), typeof(string), typeof(ConnectionCardControl), new PropertyMetadata(string.Empty));
    public static readonly DependencyProperty StatusForegroundProperty =
        DependencyProperty.Register(nameof(StatusForeground), typeof(Brush), typeof(ConnectionCardControl), new PropertyMetadata(Brushes.White));

    public static readonly DependencyProperty IpLabelProperty =
        DependencyProperty.Register(nameof(IpLabel), typeof(string), typeof(ConnectionCardControl), new PropertyMetadata(string.Empty));
    public static readonly DependencyProperty IpTextProperty =
        DependencyProperty.Register(nameof(IpText), typeof(string), typeof(ConnectionCardControl), new PropertyMetadata(string.Empty));
    public static readonly DependencyProperty IpCopyButtonVisibilityProperty =
        DependencyProperty.Register(nameof(IpCopyButtonVisibility), typeof(Visibility), typeof(ConnectionCardControl), new PropertyMetadata(Visibility.Collapsed));
    public static readonly DependencyProperty CopyToolTipProperty =
        DependencyProperty.Register(nameof(CopyToolTip), typeof(string), typeof(ConnectionCardControl), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty ServerLabelProperty =
        DependencyProperty.Register(nameof(ServerLabel), typeof(string), typeof(ConnectionCardControl), new PropertyMetadata(string.Empty));
    public static readonly DependencyProperty ServerTextProperty =
        DependencyProperty.Register(nameof(ServerText), typeof(string), typeof(ConnectionCardControl), new PropertyMetadata(string.Empty));
    public static readonly DependencyProperty PingLabelProperty =
        DependencyProperty.Register(nameof(PingLabel), typeof(string), typeof(ConnectionCardControl), new PropertyMetadata(string.Empty));
    public static readonly DependencyProperty PingTextProperty =
        DependencyProperty.Register(nameof(PingText), typeof(string), typeof(ConnectionCardControl), new PropertyMetadata(string.Empty));
    public static readonly DependencyProperty PingForegroundProperty =
        DependencyProperty.Register(nameof(PingForeground), typeof(Brush), typeof(ConnectionCardControl), new PropertyMetadata(Brushes.White));
    public static readonly DependencyProperty RegionLabelProperty =
        DependencyProperty.Register(nameof(RegionLabel), typeof(string), typeof(ConnectionCardControl), new PropertyMetadata(string.Empty));
    public static readonly DependencyProperty RegionTextProperty =
        DependencyProperty.Register(nameof(RegionText), typeof(string), typeof(ConnectionCardControl), new PropertyMetadata(string.Empty));

    public static readonly RoutedEvent CopyIpClickedEvent =
        EventManager.RegisterRoutedEvent(nameof(CopyIpClicked), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ConnectionCardControl));

    public event RoutedEventHandler CopyIpClicked
    {
        add => AddHandler(CopyIpClickedEvent, value);
        remove => RemoveHandler(CopyIpClickedEvent, value);
    }

    public ConnectionCardControl()
    {
        InitializeComponent();
    }

    public string Icon { get => (string)GetValue(IconProperty); set => SetValue(IconProperty, value); }
    public string HeaderText { get => (string)GetValue(HeaderTextProperty); set => SetValue(HeaderTextProperty, value); }
    public string StatusLabel { get => (string)GetValue(StatusLabelProperty); set => SetValue(StatusLabelProperty, value); }
    public string StatusText { get => (string)GetValue(StatusTextProperty); set => SetValue(StatusTextProperty, value); }
    public Brush StatusForeground { get => (Brush)GetValue(StatusForegroundProperty); set => SetValue(StatusForegroundProperty, value); }
    public string IpLabel { get => (string)GetValue(IpLabelProperty); set => SetValue(IpLabelProperty, value); }
    public string IpText { get => (string)GetValue(IpTextProperty); set => SetValue(IpTextProperty, value); }
    public Visibility IpCopyButtonVisibility { get => (Visibility)GetValue(IpCopyButtonVisibilityProperty); set => SetValue(IpCopyButtonVisibilityProperty, value); }
    public string CopyToolTip { get => (string)GetValue(CopyToolTipProperty); set => SetValue(CopyToolTipProperty, value); }
    public string ServerLabel { get => (string)GetValue(ServerLabelProperty); set => SetValue(ServerLabelProperty, value); }
    public string ServerText { get => (string)GetValue(ServerTextProperty); set => SetValue(ServerTextProperty, value); }
    public string PingLabel { get => (string)GetValue(PingLabelProperty); set => SetValue(PingLabelProperty, value); }
    public string PingText { get => (string)GetValue(PingTextProperty); set => SetValue(PingTextProperty, value); }
    public Brush PingForeground { get => (Brush)GetValue(PingForegroundProperty); set => SetValue(PingForegroundProperty, value); }
    public string RegionLabel { get => (string)GetValue(RegionLabelProperty); set => SetValue(RegionLabelProperty, value); }
    public string RegionText { get => (string)GetValue(RegionTextProperty); set => SetValue(RegionTextProperty, value); }

    private void IpRow_CopyClicked(object sender, RoutedEventArgs e)
    {
        RaiseEvent(new RoutedEventArgs(CopyIpClickedEvent));
    }
}
