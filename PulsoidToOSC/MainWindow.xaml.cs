using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
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

		private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
		{
			DragMove();
		}

		private void Minimize_Click(object sender, RoutedEventArgs e)
		{
			WindowState = WindowState.Minimized;
		}

		private void Close_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}

		[DllImport("dwmapi.dll")]
		private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

		const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
		const int DWMWCP_ROUND = 2;

		protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);
			
			var hwnd = new WindowInteropHelper(this).Handle;
			int preference = DWMWCP_ROUND;

			_ = DwmSetWindowAttribute(
				hwnd,
				DWMWA_WINDOW_CORNER_PREFERENCE,
				ref preference,
				sizeof(int));

			LoadWindowSettings();
		}

		protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
		{
			SaveWindowSettings();

			base.OnClosing(e);
		}

		// Window Position Saving
		private const int LayoutCountToRemember = 5;
		private static WindowSettings? settings;
		private static readonly string settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WindowPositions.json");
		private readonly JsonSerializerOptions JsonSerializerOptions = new()
		{
			WriteIndented = true
		};

		private class WindowSettings
		{
			public Dictionary<string, WindowPosition> MonitorSetups { get; set; } = [];
		}

		private class WindowPosition
		{
			public int Order { get; set; }
			public double Left { get; set; }
			public double Top { get; set; }
		}

		private static string GetMonitorLayout()
		{
			return $"{SystemParameters.VirtualScreenWidth}x{SystemParameters.VirtualScreenHeight}@{SystemParameters.VirtualScreenLeft},{SystemParameters.VirtualScreenTop}";
		}

		private void LoadWindowSettings()
		{
			if (File.Exists(settingsPath))
			{
				try { settings = JsonSerializer.Deserialize<WindowSettings>(File.ReadAllText(settingsPath)); } 
				catch { settings = null; }
			}

			string layout = GetMonitorLayout();

			if (settings == null) return;
			if (settings.MonitorSetups.TryGetValue(layout, out WindowPosition? pos))
			{
				if (pos.Left < SystemParameters.VirtualScreenLeft) pos.Left = SystemParameters.VirtualScreenLeft;
				if (pos.Left + Width > SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth) pos.Left = SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - Width;
				if (pos.Top < SystemParameters.VirtualScreenTop) pos.Top = SystemParameters.VirtualScreenTop;
				if (pos.Top + Height > SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight) pos.Top = SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight - Height;

				Left = pos.Left;
				Top = pos.Top;
			}
		}
		
		private void SaveWindowSettings()
		{
			string layout = GetMonitorLayout();
			settings ??= new WindowSettings();
			settings.MonitorSetups.Remove(layout);
			List<KeyValuePair<string, WindowPosition>> orderedItems = settings.MonitorSetups.OrderBy(x => x.Value.Order).Take(LayoutCountToRemember - 1).ToList();
			int newOrder = 1;
			foreach (KeyValuePair<string, WindowPosition> item in orderedItems )
			{
				item.Value.Order = newOrder++;
			}
			settings.MonitorSetups = orderedItems.ToDictionary();
			settings.MonitorSetups.Add(layout, new WindowPosition
			{
				Order = 0,
				Left = Left,
				Top = Top
			});
			File.WriteAllText(settingsPath, JsonSerializer.Serialize(settings, JsonSerializerOptions));
		}
	}
}