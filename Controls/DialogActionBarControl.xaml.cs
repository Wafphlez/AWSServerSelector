using System.Windows;
using System.Windows.Controls;

namespace AWSServerSelector.Controls;

public partial class DialogActionBarControl : UserControl
{
    public static readonly DependencyProperty PrimaryButtonTextProperty =
        DependencyProperty.Register(nameof(PrimaryButtonText), typeof(string), typeof(DialogActionBarControl), new PropertyMetadata(string.Empty));
    public static readonly DependencyProperty SecondaryButtonTextProperty =
        DependencyProperty.Register(nameof(SecondaryButtonText), typeof(string), typeof(DialogActionBarControl), new PropertyMetadata(string.Empty));
    public static readonly DependencyProperty PrimaryButtonVisibilityProperty =
        DependencyProperty.Register(nameof(PrimaryButtonVisibility), typeof(Visibility), typeof(DialogActionBarControl), new PropertyMetadata(Visibility.Visible));
    public static readonly DependencyProperty SecondaryButtonVisibilityProperty =
        DependencyProperty.Register(nameof(SecondaryButtonVisibility), typeof(Visibility), typeof(DialogActionBarControl), new PropertyMetadata(Visibility.Visible));
    public static readonly DependencyProperty PrimaryButtonWidthProperty =
        DependencyProperty.Register(nameof(PrimaryButtonWidth), typeof(double), typeof(DialogActionBarControl), new PropertyMetadata(110d));
    public static readonly DependencyProperty SecondaryButtonWidthProperty =
        DependencyProperty.Register(nameof(SecondaryButtonWidth), typeof(double), typeof(DialogActionBarControl), new PropertyMetadata(140d));
    public static readonly DependencyProperty ButtonHeightProperty =
        DependencyProperty.Register(nameof(ButtonHeight), typeof(double), typeof(DialogActionBarControl), new PropertyMetadata(32d));

    public static readonly RoutedEvent PrimaryClickEvent =
        EventManager.RegisterRoutedEvent(nameof(PrimaryClick), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(DialogActionBarControl));
    public static readonly RoutedEvent SecondaryClickEvent =
        EventManager.RegisterRoutedEvent(nameof(SecondaryClick), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(DialogActionBarControl));

    public event RoutedEventHandler PrimaryClick
    {
        add => AddHandler(PrimaryClickEvent, value);
        remove => RemoveHandler(PrimaryClickEvent, value);
    }

    public event RoutedEventHandler SecondaryClick
    {
        add => AddHandler(SecondaryClickEvent, value);
        remove => RemoveHandler(SecondaryClickEvent, value);
    }

    public DialogActionBarControl()
    {
        InitializeComponent();
    }

    public string PrimaryButtonText { get => (string)GetValue(PrimaryButtonTextProperty); set => SetValue(PrimaryButtonTextProperty, value); }
    public string SecondaryButtonText { get => (string)GetValue(SecondaryButtonTextProperty); set => SetValue(SecondaryButtonTextProperty, value); }
    public Visibility PrimaryButtonVisibility { get => (Visibility)GetValue(PrimaryButtonVisibilityProperty); set => SetValue(PrimaryButtonVisibilityProperty, value); }
    public Visibility SecondaryButtonVisibility { get => (Visibility)GetValue(SecondaryButtonVisibilityProperty); set => SetValue(SecondaryButtonVisibilityProperty, value); }
    public double PrimaryButtonWidth { get => (double)GetValue(PrimaryButtonWidthProperty); set => SetValue(PrimaryButtonWidthProperty, value); }
    public double SecondaryButtonWidth { get => (double)GetValue(SecondaryButtonWidthProperty); set => SetValue(SecondaryButtonWidthProperty, value); }
    public double ButtonHeight { get => (double)GetValue(ButtonHeightProperty); set => SetValue(ButtonHeightProperty, value); }

    private void PrimaryButton_Click(object sender, RoutedEventArgs e)
    {
        RaiseEvent(new RoutedEventArgs(PrimaryClickEvent));
    }

    private void SecondaryButton_Click(object sender, RoutedEventArgs e)
    {
        RaiseEvent(new RoutedEventArgs(SecondaryClickEvent));
    }
}
