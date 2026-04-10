using System.Configuration;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AWSServerSelector.Models;
using AWSServerSelector.Services;
using AWSServerSelector.Services.Interfaces;
using AWSServerSelector.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AWSServerSelector
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static IServiceProvider Services { get; private set; } = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            Services = ConfigureServices();

            // Register command bindings for custom title bar
            EventManager.RegisterClassHandler(typeof(Window), Window.LoadedEvent, new RoutedEventHandler(OnWindowLoaded));

            var mainWindow = Services.GetRequiredService<MainWindow>();
            MainWindow = mainWindow;
            mainWindow.Show();
        }

        private static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();
            var configuration = BuildConfiguration();

            services.AddSingleton<IConfiguration>(configuration);
            services.Configure<AppLinksOptions>(configuration.GetSection(AppLinksOptions.SectionName));
            services.Configure<UpdateOptions>(configuration.GetSection(UpdateOptions.SectionName));
            services.Configure<MonitoringOptions>(configuration.GetSection(MonitoringOptions.SectionName));
            services.Configure<HostsOptions>(configuration.GetSection(HostsOptions.SectionName));
            services.Configure<RegionCatalogOptions>(configuration.GetSection(RegionCatalogOptions.SectionName));

            services.AddSingleton<ISettingsService, JsonSettingsService>();
            services.AddSingleton<IHostsService, HostsService>();
            services.AddSingleton<IUpdateService, UpdateService>();
            services.AddSingleton<ILatencyService, LatencyService>();
            services.AddSingleton<IAwsIpRangeService, AwsIpRangeService>();
            services.AddSingleton<IConnectionMonitorService, ConnectionMonitorService>();
            services.AddSingleton<IDialogService, DialogService>();
            services.AddSingleton<IRegionCatalogService, RegionCatalogService>();
            services.AddSingleton<IMessageService, MessageService>();
            services.AddSingleton<IExternalNavigationService, ExternalNavigationService>();
            services.AddSingleton<IClipboardService, ClipboardService>();

            services.AddSingleton<MainWindow>();
            services.AddTransient<SettingsDialogViewModel>();
            services.AddTransient<UpdateDialogViewModel>();
            services.AddTransient<AboutDialogViewModel>();
            services.AddTransient<SettingsDialog>();
            services.AddTransient<UpdateDialog>();
            services.AddTransient<AboutDialog>();
            services.AddTransient<ConnectionInfoWindow>();

            return services.BuildServiceProvider();
        }

        private static IConfiguration BuildConfiguration()
        {
            try
            {
                return new ConfigurationBuilder()
                    .SetBasePath(AppContext.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                    .AddJsonFile("Config/regions.json", optional: true, reloadOnChange: false)
                    .Build();
            }
            catch (Exception ex)
            {
                AppLogger.Error("Configuration load failed. Using built-in defaults.", ex);
                return new ConfigurationBuilder().Build();
            }
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
