using System.Windows;
using AWSServerSelector.ViewModels;

namespace AWSServerSelector
{
    public partial class AboutDialog : Window
    {
        private readonly AboutDialogViewModel _viewModel;

        public string AboutText
        {
            get => _viewModel.AboutText;
            set => _viewModel.AboutText = value;
        }

        public string Developer
        {
            get => _viewModel.Developer;
            set => _viewModel.Developer = value;
        }

        public string VersionText
        {
            get => _viewModel.VersionText;
            set => _viewModel.VersionText = value;
        }

        public string AwesomeText
        {
            get => _viewModel.AwesomeText;
            set => _viewModel.AwesomeText = value;
        }

        public AboutDialog(AboutDialogViewModel viewModel)
        {
            _viewModel = viewModel;
            InitializeComponent();
            DataContext = _viewModel;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
