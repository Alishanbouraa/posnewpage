using QuickTechPOS.Helpers;
using System.Windows;
using System.Linq;

namespace QuickTechPOS.Views
{
    public partial class LanguageSettingsDialog : Window
    {
        private string selectedLanguageCode;

        public LanguageSettingsDialog()
        {
            InitializeComponent();

            // Apply current flow direction
            this.FlowDirection = LanguageManager.CurrentFlowDirection;

            // Load available languages
            LanguageComboBox.ItemsSource = LanguageManager.AvailableLanguages;

            // Set currently selected language
            selectedLanguageCode = LanguageManager.CurrentLanguageCode;
            LanguageComboBox.SelectedItem = LanguageManager.AvailableLanguages
                .FirstOrDefault(l => l.Code == selectedLanguageCode);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (LanguageComboBox.SelectedItem is LanguageOption selectedLanguage)
            {
                selectedLanguageCode = selectedLanguage.Code;

                if (selectedLanguageCode != LanguageManager.CurrentLanguageCode)
                {
                    LanguageManager.ChangeLanguage(selectedLanguageCode);

                    // Apply flow direction to this dialog immediately
                    this.FlowDirection = LanguageManager.CurrentFlowDirection;

                    // Apply to main window too if it exists
                    if (Application.Current?.MainWindow != null)
                    {
                        Application.Current.MainWindow.FlowDirection = LanguageManager.CurrentFlowDirection;
                    }

                    MessageBox.Show(FindResource("LanguageChangeSuccess") as string,
                        FindResource("SuccessTitle") as string,
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }

                DialogResult = true;
                Close();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}