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
using Microsoft.Extensions.Options;

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
            try
            {
                Services = ConfigureServices();
            }
            catch (Exception ex)
            {
                HandleStartupFailure(ex);
                return;
            }

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
            services.AddOptions<AppLinksOptions>()
                .Bind(configuration.GetSection(AppLinksOptions.SectionName))
                .Validate(options =>
                    !string.IsNullOrWhiteSpace(options.DiscordUrl) &&
                    Uri.TryCreate(options.DiscordUrl, UriKind.Absolute, out _),
                    "AppLinks.DiscordUrl must be a valid absolute URL.")
                .ValidateOnStart();

            services.AddOptions<UpdateOptions>()
                .Bind(configuration.GetSection(UpdateOptions.SectionName))
                .Validate(options =>
                    !string.IsNullOrWhiteSpace(options.LatestReleaseApiUrl) &&
                    Uri.TryCreate(options.LatestReleaseApiUrl, UriKind.Absolute, out _),
                    "Update.LatestReleaseApiUrl must be a valid absolute URL.")
                .Validate(options => !string.IsNullOrWhiteSpace(options.UserAgent),
                    "Update.UserAgent must not be empty.")
                .Validate(options => options.TimeoutSeconds >= 3 && options.TimeoutSeconds <= 120,
                    "Update.TimeoutSeconds must be between 3 and 120.")
                .ValidateOnStart();

            services.AddOptions<MonitoringOptions>()
                .Bind(configuration.GetSection(MonitoringOptions.SectionName))
                .Validate(options => options.MainPingIntervalSeconds >= 1 && options.MainPingIntervalSeconds <= 120,
                    "Monitoring.MainPingIntervalSeconds must be between 1 and 120.")
                .Validate(options => options.MainPingTimeoutMs >= 250 && options.MainPingTimeoutMs <= 10000,
                    "Monitoring.MainPingTimeoutMs must be between 250 and 10000.")
                .Validate(options => options.ConnectionPollIntervalSeconds >= 1 && options.ConnectionPollIntervalSeconds <= 120,
                    "Monitoring.ConnectionPollIntervalSeconds must be between 1 and 120.")
                .Validate(options => options.ConnectionPingTimeoutMs >= 250 && options.ConnectionPingTimeoutMs <= 10000,
                    "Monitoring.ConnectionPingTimeoutMs must be between 250 and 10000.")
                .Validate(options => options.ConnectionGamePingIntervalSeconds >= 1 && options.ConnectionGamePingIntervalSeconds <= 10,
                    "Monitoring.ConnectionGamePingIntervalSeconds must be between 1 and 10.")
                .Validate(options => options.IpApiTimeoutSeconds >= 2 && options.IpApiTimeoutSeconds <= 30,
                    "Monitoring.IpApiTimeoutSeconds must be between 2 and 30.")
                .ValidateOnStart();

            services.AddOptions<HostsOptions>()
                .Bind(configuration.GetSection(HostsOptions.SectionName))
                .Validate(options => !string.IsNullOrWhiteSpace(options.DefaultHostsTemplatePath),
                    "Hosts.DefaultHostsTemplatePath must not be empty.")
                .ValidateOnStart();

            services.AddOptions<RegionCatalogOptions>()
                .Bind(configuration.GetSection(RegionCatalogOptions.SectionName))
                .Validate(options => options.Regions.Count > 0,
                    "RegionCatalog.Regions must contain at least one region.")
                .Validate(options => options.Regions.All(region =>
                    !string.IsNullOrWhiteSpace(region.Key) &&
                    !string.IsNullOrWhiteSpace(region.GroupKey) &&
                    !string.IsNullOrWhiteSpace(region.GroupDisplayName) &&
                    !string.IsNullOrWhiteSpace(region.DisplayNameKey) &&
                    region.Hosts.Length > 0 &&
                    region.Hosts.All(host => !string.IsNullOrWhiteSpace(host))),
                    "RegionCatalog.Regions contains invalid entries.")
                .ValidateOnStart();

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

            var provider = services.BuildServiceProvider();
            ValidateOptionsOnStartup(provider);
            return provider;
        }

        private static IConfiguration BuildConfiguration()
        {
            return new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .AddJsonFile("Config/regions.json", optional: false, reloadOnChange: false)
                .Build();
        }

        private static void HandleStartupFailure(Exception ex)
        {
            AppLogger.Error("Application startup failed due to configuration error.", ex);
            MessageBox.Show(
                "Не удалось запустить приложение из-за ошибки конфигурации.\n" +
                "Проверьте файлы appsettings.json и Config/regions.json.\n\n" +
                $"Детали: {ex.Message}",
                "Ошибка конфигурации",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Current?.Shutdown(-1);
        }

        private static void ValidateOptionsOnStartup(IServiceProvider provider)
        {
            _ = provider.GetRequiredService<IOptions<AppLinksOptions>>().Value;
            _ = provider.GetRequiredService<IOptions<UpdateOptions>>().Value;
            _ = provider.GetRequiredService<IOptions<MonitoringOptions>>().Value;
            _ = provider.GetRequiredService<IOptions<HostsOptions>>().Value;
            _ = provider.GetRequiredService<IOptions<RegionCatalogOptions>>().Value;
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
