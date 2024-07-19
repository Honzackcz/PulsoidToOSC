using System.Windows;

namespace PulsoidToOSC
{
    /// <summary>
    /// Interakční logika pro OptionsWindow.xaml
    /// </summary>
    public partial class OptionsWindow : Window
    {
        public OptionsWindow()
        {
            InitializeComponent();
        }

        private void GotFocusToken(object sender, RoutedEventArgs e)
        {
            TokenBox.Visibility = Visibility.Visible;
            TokenHiddenBox.Visibility = Visibility.Collapsed;
			MainProgram.MainViewModel.TokenText = ConfigData.PulsoidToken;
			TokenBox.Focus();
        }

        private void LostFocusToken(object sender, RoutedEventArgs e)
        {
			TokenBox.Visibility = Visibility.Collapsed;
			TokenHiddenBox.Visibility = Visibility.Visible;
		}
	}
}