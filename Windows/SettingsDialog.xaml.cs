using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace AWSServerSelector
{
    public partial class SettingsDialog : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private string _title = "Settings";
        public new string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public string SettingsTitle => LocalizationManager.GetString("Settings");

        private string _selectedLanguage = "en"; // Default to English
        public string SelectedLanguage
        {
            get => _selectedLanguage;
            set => SetProperty(ref _selectedLanguage, value);
        }

        private string _selectedMode = "hosts";
        public string SelectedMode
        {
            get => _selectedMode;
            set => SetProperty(ref _selectedMode, value);
        }

        private bool _isBlockBoth = true;
        public bool IsBlockBoth
        {
            get => _isBlockBoth;
            set => SetProperty(ref _isBlockBoth, value);
        }

        private bool _isBlockPing = false;
        public bool IsBlockPing
        {
            get => _isBlockPing;
            set => SetProperty(ref _isBlockPing, value);
        }

        private bool _isBlockService = false;
        public bool IsBlockService
        {
            get => _isBlockService;
            set => SetProperty(ref _isBlockService, value);
        }

        private bool _isMergeUnstable = false;
        public bool IsMergeUnstable
        {
            get => _isMergeUnstable;
            set => SetProperty(ref _isMergeUnstable, value);
        }

        public List<LanguageItem> Languages { get; } = new List<LanguageItem>
        {
            new LanguageItem { Code = "en", Name = "English" },
            new LanguageItem { Code = "ru", Name = "Русский" }
        };

        public List<ModeItem> Modes { get; } = new List<ModeItem>
        {
            new ModeItem { Code = "hosts", Name = "Hosts File" },
            new ModeItem { Code = "service", Name = "Service" }
        };

        public string LanguageText => LocalizationManager.GetString("Language");
        public string MethodText => LocalizationManager.GetString("Method");
        public string GatekeepOptionsText => LocalizationManager.GetString("GatekeepOptions");
        public string BlockBothText => LocalizationManager.GetString("BlockBoth");
        public string BlockPingText => LocalizationManager.GetString("BlockPing");
        public string BlockServiceText => LocalizationManager.GetString("BlockService");
        public string MergeUnstableText => LocalizationManager.GetString("MergeUnstable");
        public string DefaultOptionsText => LocalizationManager.GetString("DefaultOptions");
        public string ApplyChangesText => LocalizationManager.GetString("ApplyChanges");

        public SettingsDialog()
        {
            InitializeComponent();
            DataContext = this;
            
            // Subscribe to localization changes
            LocalizationManager.LanguageChanged += OnLanguageChanged;
        }

        private void OnLanguageChanged(object? sender, EventArgs e)
        {
            // Update localized strings
            OnPropertyChanged(nameof(LanguageText));
            OnPropertyChanged(nameof(MethodText));
            OnPropertyChanged(nameof(GatekeepOptionsText));
            OnPropertyChanged(nameof(BlockBothText));
            OnPropertyChanged(nameof(BlockPingText));
            OnPropertyChanged(nameof(BlockServiceText));
            OnPropertyChanged(nameof(MergeUnstableText));
            OnPropertyChanged(nameof(DefaultOptionsText));
            OnPropertyChanged(nameof(ApplyChangesText));
            OnPropertyChanged(nameof(SettingsTitle));
            OnPropertyChanged(nameof(Title));
        }

        private void DefaultButton_Click(object sender, RoutedEventArgs e)
        {
            // Reset to default values
            SelectedLanguage = "en";
            SelectedMode = "hosts";
            IsBlockBoth = true;
            IsBlockPing = false;
            IsBlockService = false;
            IsMergeUnstable = false;
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(T value, ref T field, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected override void OnClosed(EventArgs e)
        {
            // Unsubscribe from events
            LocalizationManager.LanguageChanged -= OnLanguageChanged;
            base.OnClosed(e);
        }
    }

    public class LanguageItem
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public class ModeItem
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}
