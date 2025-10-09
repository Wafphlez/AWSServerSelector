using System;
using System.ComponentModel;
using System.Globalization;
using System.Resources;
using System.Windows;

namespace AWSServerSelector
{
    public static class LocalizationManager
    {
        private static ResourceManager _resourceManager;
        private static CultureInfo _currentCulture;

        static LocalizationManager()
        {
            _resourceManager = new ResourceManager("AWSServerSelector.Resources.Strings", typeof(LocalizationManager).Assembly);
            _currentCulture = CultureInfo.CurrentCulture;
        }

        // Static properties for XAML binding
        public static string AppTitle => GetString("AppTitle");
        public static string SelectServers => GetString("SelectServers");
        public static string Latency => GetString("Latency");
        public static string StatusText => GetString("StatusText");
        public static string OpenHosts => GetString("OpenHosts");
        public static string ResetToDefault => GetString("ResetToDefault");
        public static string ApplySelection => GetString("ApplySelection");
        public static string Settings => GetString("Settings");
        public static string Repository => GetString("Repository");
        public static string Website => GetString("Website");
        public static string DiscordSupport => GetString("DiscordSupport");
        public static string OpenHostsLocation => GetString("OpenHostsLocation");
        public static string ResetHosts => GetString("ResetHosts");
        public static string About => GetString("About");
        public static string SelectAll => GetString("SelectAll");
        
        // Region and server translations
        public static string Europe => GetString("Europe");
        public static string Americas => GetString("Americas");
        public static string Asia => GetString("Asia");
        public static string Oceania => GetString("Oceania");
        public static string China => GetString("China");
        public static string CheckUpdates => GetString("CheckUpdates");

        public static string GetString(string key)
        {
            try
            {
                return _resourceManager.GetString(key, _currentCulture) ?? key;
            }
            catch
            {
                return key;
            }
        }

        public static string GetString(string key, params object[] args)
        {
            try
            {
                var format = _resourceManager.GetString(key, _currentCulture) ?? key;
                return string.Format(format, args);
            }
            catch
            {
                return key;
            }
        }

        public static void SetLanguage(string languageCode)
        {
            try
            {
                _currentCulture = new CultureInfo(languageCode);
                CultureInfo.CurrentCulture = _currentCulture;
                CultureInfo.CurrentUICulture = _currentCulture;
            }
            catch
            {
                // Fallback to default culture
                _currentCulture = CultureInfo.CurrentCulture;
            }
        }

        public static string CurrentLanguage => _currentCulture.TwoLetterISOLanguageName;

        public static event EventHandler? LanguageChanged;

        public static void NotifyLanguageChanged()
        {
            LanguageChanged?.Invoke(null, EventArgs.Empty);
        }

        public static void SetLanguageAndNotify(string languageCode)
        {
            SetLanguage(languageCode);
            NotifyLanguageChanged();
            NotifyAllPropertiesChanged();
        }

        public static event PropertyChangedEventHandler? PropertyChanged;

        private static void NotifyAllPropertiesChanged()
        {
            PropertyChanged?.Invoke(null, new PropertyChangedEventArgs(string.Empty));
        }

        public static string GetServerDisplayName(string regionKey)
        {
            // Map region keys to translation keys
            var translationKey = regionKey switch
            {
                "Europe (London)" => "Europe_London",
                "Europe (Ireland)" => "Europe_Ireland", 
                "Europe (Frankfurt am Main)" => "Europe_Frankfurt",
                "US East (N. Virginia)" => "US_East_Virginia",
                "US East (Ohio)" => "US_East_Ohio",
                "US West (N. California)" => "US_West_California",
                "US West (Oregon)" => "US_West_Oregon",
                "Canada (Central)" => "Canada_Central",
                "South America (São Paulo)" => "South_America_Sao_Paulo",
                "Asia Pacific (Tokyo)" => "Asia_Tokyo",
                "Asia Pacific (Seoul)" => "Asia_Seoul",
                "Asia Pacific (Mumbai)" => "Asia_Mumbai",
                "Asia Pacific (Singapore)" => "Asia_Singapore",
                "Asia Pacific (Hong Kong)" => "Asia_Hong_Kong",
                "Asia Pacific (Sydney)" => "Asia_Sydney",
                "China (Beijing)" => "China_Beijing",
                "China (Ningxia)" => "China_Ningxia",
                _ => null
            };

            return translationKey != null ? GetString(translationKey) : regionKey;
        }

        public static string GetGroupDisplayName(string groupName)
        {
            return groupName switch
            {
                "Europe" => Europe,
                "Americas" => Americas,
                "Asia" => Asia,
                "Oceania" => Oceania,
                "China" => China,
                _ => groupName
            };
        }
    }
}
