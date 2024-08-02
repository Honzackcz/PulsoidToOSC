using System.Windows.Input;
using System.Windows;
using System.ComponentModel;
using System.Diagnostics;

namespace PulsoidToOSC
{
	public class InfoViewModel : ViewModelBase
	{
		private readonly MainViewModel _mainViewModel;
		public InfoWindow? InfoWindow { get; private set; }

		private readonly string _versionText = "Version: ";

		public string VersionText
		{
			get => _versionText + MainProgram.AppVersion;
		}

		public ICommand OpenGitHubCommand { get; }
		public ICommand OpenLicenseCommand { get; }
		public ICommand InfoOKCommand { get; }

		public InfoViewModel(MainViewModel mainViewModel)
		{
			_mainViewModel = mainViewModel;

			OpenGitHubCommand = new RelayCommand(OpenGitHub);
			OpenLicenseCommand = new RelayCommand(OpenLicense);
			InfoOKCommand = new RelayCommand(InfoOK);
		}

		public void OpenInfoWindow()
		{
			InfoWindow = new()
			{
				DataContext = this,
				Owner = _mainViewModel.MainWindow,
				WindowStartupLocation = WindowStartupLocation.CenterOwner
			};
			InfoWindow.Closing += InfoWindowClosing;
			InfoWindow.ShowDialog();
		}

		private void OpenGitHub()
		{
			Process.Start(new ProcessStartInfo
			{
				FileName = "https://github.com/Honzackcz/PulsoidToOSC",
				UseShellExecute = true
			});
		}

		private void OpenLicense()
		{
			Process.Start(new ProcessStartInfo
			{
				FileName = "https://github.com/Honzackcz/PulsoidToOSC/blob/master/LICENSE.txt",
				UseShellExecute = true
			});
		}

		private void InfoOK()
		{
			if (InfoWindow == null) return;
			InfoWindow.Close();
		}

		public void InfoWindowClosing(object? sender, CancelEventArgs e)
		{

		}
	}
}