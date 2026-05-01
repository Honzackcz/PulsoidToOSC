using System.Windows.Controls;

namespace PulsoidToOSC.Windows.Options
{
    public partial class Parameters : UserControl
    {
        public Parameters()
        {
            InitializeComponent();
        }
        
		private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			((ListView)sender).SelectedIndex = -1;
		}
	}
}
