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

        /// <summary>Сохраняет выбранный статус и подтверждает диалог</summary>
        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            SelectedStatus = cbStatus.SelectedItem?.ToString() ?? string.Empty;
            DialogResult = true;
            Close();
        }

        /// <summary>Отменяет изменение и закрывает диалог</summary>
        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}