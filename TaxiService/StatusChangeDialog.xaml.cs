using System.Windows;

namespace TaxiService.WPF
{
    public partial class StatusChangeDialog : Window
    {
        public string SelectedStatus { get; private set; } = string.Empty;

        public StatusChangeDialog(string[] statuses)
        {
            InitializeComponent();
            cbStatus.ItemsSource = statuses;
            cbStatus.SelectedIndex = 0;
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            SelectedStatus = cbStatus.SelectedItem?.ToString() ?? string.Empty;
            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}