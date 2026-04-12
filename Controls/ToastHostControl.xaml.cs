using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace AWSServerSelector.Controls;

public partial class ToastHostControl : UserControl
{
    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable), typeof(ToastHostControl), new PropertyMetadata(null));

    public static readonly DependencyProperty HostMarginProperty =
        DependencyProperty.Register(nameof(HostMargin), typeof(Thickness), typeof(ToastHostControl), new PropertyMetadata(new Thickness(0, 0, 0, 20)));

    public static readonly DependencyProperty ToastMinWidthProperty =
        DependencyProperty.Register(nameof(ToastMinWidth), typeof(double), typeof(ToastHostControl), new PropertyMetadata(340d));

    public static readonly DependencyProperty ToastMaxWidthProperty =
        DependencyProperty.Register(nameof(ToastMaxWidth), typeof(double), typeof(ToastHostControl), new PropertyMetadata(520d));

    public ToastHostControl()
    {
        InitializeComponent();
    }

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public Thickness HostMargin
    {
        get => (Thickness)GetValue(HostMarginProperty);
        set => SetValue(HostMarginProperty, value);
    }

    public double ToastMinWidth
    {
        get => (double)GetValue(ToastMinWidthProperty);
        set => SetValue(ToastMinWidthProperty, value);
    }

    public double ToastMaxWidth
    {
        get => (double)GetValue(ToastMaxWidthProperty);
        set => SetValue(ToastMaxWidthProperty, value);
    }
}
