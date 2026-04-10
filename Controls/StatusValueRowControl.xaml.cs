using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace AWSServerSelector.Controls;

public partial class StatusValueRowControl : UserControl
{
    public static readonly DependencyProperty LabelTextProperty =
        DependencyProperty.Register(nameof(LabelText), typeof(string), typeof(StatusValueRowControl), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty ValueTextProperty =
        DependencyProperty.Register(nameof(ValueText), typeof(string), typeof(StatusValueRowControl), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty ValueForegroundProperty =
        DependencyProperty.Register(nameof(ValueForeground), typeof(Brush), typeof(StatusValueRowControl), new PropertyMetadata(Brushes.White));

    public static readonly DependencyProperty ShowCopyButtonProperty =
        DependencyProperty.Register(nameof(ShowCopyButton), typeof(Visibility), typeof(StatusValueRowControl), new PropertyMetadata(Visibility.Collapsed));

    public static readonly DependencyProperty CopyToolTipProperty =
        DependencyProperty.Register(nameof(CopyToolTip), typeof(string), typeof(StatusValueRowControl), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty RowMarginProperty =
        DependencyProperty.Register(nameof(RowMargin), typeof(Thickness), typeof(StatusValueRowControl), new PropertyMetadata(new Thickness(0, 0, 0, 5)));

    public static readonly RoutedEvent CopyClickedEvent =
        EventManager.RegisterRoutedEvent(nameof(CopyClicked), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(StatusValueRowControl));

    public event RoutedEventHandler CopyClicked
    {
        add => AddHandler(CopyClickedEvent, value);
        remove => RemoveHandler(CopyClickedEvent, value);
    }

    public StatusValueRowControl()
    {
        InitializeComponent();
    }

    public string LabelText
    {
        get => (string)GetValue(LabelTextProperty);
        set => SetValue(LabelTextProperty, value);
    }

    public string ValueText
    {
        get => (string)GetValue(ValueTextProperty);
        set => SetValue(ValueTextProperty, value);
    }

    public Brush ValueForeground
    {
        get => (Brush)GetValue(ValueForegroundProperty);
        set => SetValue(ValueForegroundProperty, value);
    }

    public Visibility ShowCopyButton
    {
        get => (Visibility)GetValue(ShowCopyButtonProperty);
        set => SetValue(ShowCopyButtonProperty, value);
    }

    public string CopyToolTip
    {
        get => (string)GetValue(CopyToolTipProperty);
        set => SetValue(CopyToolTipProperty, value);
    }

    public Thickness RowMargin
    {
        get => (Thickness)GetValue(RowMarginProperty);
        set => SetValue(RowMarginProperty, value);
    }

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        RaiseEvent(new RoutedEventArgs(CopyClickedEvent));
    }
}
