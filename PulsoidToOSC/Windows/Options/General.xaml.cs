using System.Windows;
using System.Windows.Controls;

namespace PulsoidToOSC.Windows.Options
{
    public partial class General : UserControl
    {
        public General()
        {
            InitializeComponent();
        }

		private void GotFocusToken(object sender, RoutedEventArgs e)
		{
			MainProgram.MainViewModel.OptionsViewModel.GeneralViewModel.TokenTextBoxInFocus = TokenBox.IsFocused;
			TokenBox.CaretIndex = int.MaxValue;
		}

		private void LostFocusToken(object sender, RoutedEventArgs e)
		{
			MainProgram.MainViewModel.OptionsViewModel.GeneralViewModel.TokenTextBoxInFocus = TokenBox.IsFocused;
		}
	}
}
