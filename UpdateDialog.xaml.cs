using System.Windows;

namespace AWSServerSelector
{
    public partial class UpdateDialog : Window
    {
        public string StatusText { get; set; } = "Проверяем наличие обновлений...";

        public UpdateDialog()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}

