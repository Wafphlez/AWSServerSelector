using System.Windows;

namespace AWSServerSelector
{
    public partial class AboutDialog : Window
    {
        public string AboutText { get; set; } = "";
        public string Developer { get; set; } = "";
        public string VersionText { get; set; } = "";
        public string AwesomeText { get; set; } = "";

        public AboutDialog()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
