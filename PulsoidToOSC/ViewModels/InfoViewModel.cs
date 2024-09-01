using System.Windows.Input;
using System.Windows;
using System.ComponentModel;
using System.Diagnostics;

namespace PulsoidToOSC
{
	internal class InfoViewModel : ViewModelBase
	{
		public InfoWindow? InfoWindow { get; private set; }

		private readonly MainViewModel _mainViewModel;
		private readonly string _versionText = "Version: ";
		private bool _isNewVersionAvailable = false;

		public bool IsNewVersionAvailable
		{
			get => _isNewVersionAvailable;
			set { _isNewVersionAvailable = value; OnPropertyChanged(nameof(NewVersionIndicator)); }
		}
		public string VersionText
		{
			get => _versionText + MainProgram.AppVersion;
		}
		public string NewVersionIndicator
		{
			get => _isNewVersionAvailable ? "Visible" : "Collapsed";
		}

		public ICommand OpenGitHubCommand { get; }
		public ICommand OpenGitHubReleasesCommand { get; }
		public ICommand OpenGitHubLicenseCommand { get; }
		public ICommand InfoOKCommand { get; }

		public InfoViewModel(MainViewModel mainViewModel)
		{
			_mainViewModel = mainViewModel;

			OpenGitHubCommand = new RelayCommand(OpenGitHub);
			OpenGitHubReleasesCommand = new RelayCommand(OpenGitHubReleases);
			OpenGitHubLicenseCommand = new RelayCommand(OpenGitHubLicense);
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

		private void OpenGitHubReleases()
		{
			Process.Start(new ProcessStartInfo
			{
				FileName = "https://github.com/Honzackcz/PulsoidToOSC/releases/latest",
				UseShellExecute = true
			});
		}

		private void OpenGitHubLicense()
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