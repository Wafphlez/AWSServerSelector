using System.Configuration;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AWSServerSelector
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // Register command bindings for custom title bar
            EventManager.RegisterClassHandler(typeof(Window), Window.LoadedEvent, new RoutedEventHandler(OnWindowLoaded));
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is Window window)
            {
                // Add command bindings for system commands
                window.CommandBindings.Add(new CommandBinding(SystemCommands.MinimizeWindowCommand, 
                    (s, args) => SystemCommands.MinimizeWindow(window)));
                window.CommandBindings.Add(new CommandBinding(SystemCommands.MaximizeWindowCommand, 
                    (s, args) => SystemCommands.MaximizeWindow(window)));
                window.CommandBindings.Add(new CommandBinding(SystemCommands.RestoreWindowCommand, 
                    (s, args) => SystemCommands.RestoreWindow(window)));
                window.CommandBindings.Add(new CommandBinding(SystemCommands.CloseWindowCommand, 
                    (s, args) => SystemCommands.CloseWindow(window)));
                
                // Subscribe to state changed event
                window.StateChanged += (s, args) => UpdateMaximizeRestoreButton(window);
                
                // Update maximize/restore button state initially
                UpdateMaximizeRestoreButton(window);
            }
        }

        private void UpdateMaximizeRestoreButton(Window window)
        {
            Button? maxButton = window.Template?.FindName("MaximizeRestoreButton", window) as Button;
            if (maxButton != null)
            {
                maxButton.Content = window.WindowState == WindowState.Maximized ? "❐" : "☐";
            }
        }

        private void MaximizeRestoreButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                Window? window = Window.GetWindow(button);
                if (window != null)
                {
                    if (window.WindowState == WindowState.Maximized)
                    {
                        SystemCommands.RestoreWindow(window);
                    }
                    else
                    {
                        SystemCommands.MaximizeWindow(window);
                    }
                }
            }
        }
    }

}
