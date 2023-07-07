using System.Windows;

namespace Tagger.Pages
{
    /// <summary>
    /// Логика взаимодействия для VerificationCodeDialogPage.xaml
    /// </summary>
    public partial class VerificationCodeDialogPage : Window
    {
        public VerificationCodeDialogPage(string phone)
        {
            InitializeComponent();
            Phone.Content += phone;
        }

        public string Code { get => CodeTB.Text; }

        private void AcceptButtonClick(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
