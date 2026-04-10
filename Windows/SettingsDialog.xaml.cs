using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using AWSServerSelector.ViewModels;

namespace AWSServerSelector
{
    public partial class SettingsDialog : Window, INotifyPropertyChanged
    {
        private readonly SettingsDialogViewModel _viewModel;
        public event PropertyChangedEventHandler? PropertyChanged;

        private string _title = "Settings";
        public new string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public string SettingsTitle => LocalizationManager.GetString("Settings");

        public string SelectedLanguage
        {
            get => _viewModel.SelectedLanguage;
            set => _viewModel.SelectedLanguage = value;
        }

        public string SelectedMode
        {
            get => _viewModel.SelectedMode;
            set => _viewModel.SelectedMode = value;
        }

        public bool IsBlockBoth
        {
            get => _viewModel.IsBlockBoth;
            set => _viewModel.IsBlockBoth = value;
        }

        public bool IsBlockPing
        {
            get => _viewModel.IsBlockPing;
            set => _viewModel.IsBlockPing = value;
        }

        public bool IsBlockService
        {
            get => _viewModel.IsBlockService;
            set => _viewModel.IsBlockService = value;
        }

        public bool IsMergeUnstable
        {
            get => _viewModel.IsMergeUnstable;
            set => _viewModel.IsMergeUnstable = value;
        }

        public List<LanguageItem> Languages => _viewModel.Languages;
        public List<ModeItem> Modes => _viewModel.Modes;

        public string LanguageText => LocalizationManager.GetString("Language");
        public string MethodText => LocalizationManager.GetString("Method");
        public string GatekeepOptionsText => LocalizationManager.GetString("GatekeepOptions");
        public string BlockBothText => LocalizationManager.GetString("BlockBoth");
        public string BlockPingText => LocalizationManager.GetString("BlockPing");
        public string BlockServiceText => LocalizationManager.GetString("BlockService");
        public string MergeUnstableText => LocalizationManager.GetString("MergeUnstable");
        public string DefaultOptionsText => LocalizationManager.GetString("DefaultOptions");
        public string ApplyChangesText => LocalizationManager.GetString("ApplyChanges");

        public SettingsDialog(SettingsDialogViewModel viewModel)
        {
            _viewModel = viewModel;
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

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
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
