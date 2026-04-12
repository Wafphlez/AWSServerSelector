using System.Windows;
using AWSServerSelector.ViewModels;

namespace AWSServerSelector
{
    public partial class AboutDialog : Window
    {
        private readonly AboutDialogViewModel _viewModel;

        public AboutDialogViewModel ViewModel => _viewModel;

        public AboutDialog(AboutDialogViewModel viewModel)
        {
            _viewModel = viewModel;
            InitializeComponent();
            DataContext = _viewModel;
            Title = _viewModel.DialogTitle;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
