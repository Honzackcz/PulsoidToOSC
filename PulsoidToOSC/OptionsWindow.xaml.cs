using System.Windows;
using System.Windows.Controls;

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
            TokenHiddenBox.Visibility = Visibility.Hidden;
			MainProgram.MainViewModel.TokenText = ConfigData.PulsoidToken;
			TokenBox.Focus();
            TokenBox.CaretIndex = int.MaxValue;
        }

        private void LostFocusToken(object sender, RoutedEventArgs e)
        {
			TokenBox.Visibility = Visibility.Hidden;
			TokenHiddenBox.Visibility = Visibility.Visible;
		}

		private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			((ListView)sender).SelectedIndex = -1;
		}
	}
}