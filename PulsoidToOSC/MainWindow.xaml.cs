using System.Windows;
using System.Windows.Interop;

namespace PulsoidToOSC
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
			SourceInitialized += MainWindow_SourceInitialized;
		}

		private void MainWindow_SourceInitialized(object? sender, EventArgs e)
		{
			HwndSource hwndSource = (HwndSource)PresentationSource.FromVisual(this);
			hwndSource.AddHook(WndProc);
		}

		private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
		{
			const int WM_SYSCOMMAND = 0x112;
			const int SC_MAXIMIZE = 0xF030;

			if (msg == WM_SYSCOMMAND && wParam.ToInt32() == SC_MAXIMIZE)
			{
				handled = true;
				return IntPtr.Zero;
			}

			return IntPtr.Zero;
		}
	}
}