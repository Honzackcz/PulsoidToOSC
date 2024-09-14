using System.Net;
using System.Windows.Input;

namespace PulsoidToOSC
{
	internal class OptionsOscViewModel : ViewModelBase
	{
		private readonly OptionsViewModel _optionsViewModel;

		private bool _oscManualConfigCheckmark = false;
		private string _oscIpText = string.Empty;
		private string _oscPortText = string.Empty;
		private string _oscPathText = string.Empty;

		public bool OSCManualConfigCheckmark
		{
			get => _oscManualConfigCheckmark;
			set { _oscManualConfigCheckmark = value; OnPropertyChanged(); ToggleOSCManualConfig(); }
		}
		public string OSCIPText
		{
			get => _oscIpText;
			set { _oscIpText = value ?? string.Empty; OnPropertyChanged(); }
		}
		public string OSCPortText
		{
			get => _oscPortText;
			set
			{
				string newText = value ?? string.Empty;

				if (newText == string.Empty)
				{
					_oscPortText = newText;
				}
				else if (Int32.TryParse(newText, out int result))
				{
					_oscPortText = Math.Clamp(result, 1, 65535).ToString();
				}

				OnPropertyChanged();
			}
		}
		public string OSCPathText
		{
			get => _oscPathText;
			set { _oscPathText = value?.Replace("=", string.Empty) ?? string.Empty; OnPropertyChanged(); }
		}

		public ICommand OptionsOSCApplyCommand { get; }

		public OptionsOscViewModel(OptionsViewModel optionsViewModel)
		{
			_optionsViewModel = optionsViewModel;
			OptionsOSCApplyCommand = new RelayCommand(_optionsViewModel.OptionsApply);
		}

		private void ToggleOSCManualConfig()
		{
			if (ConfigData.OSCUseManualConfig == OSCManualConfigCheckmark) return;
			ConfigData.OSCUseManualConfig = OSCManualConfigCheckmark;
			ConfigData.SaveConfig();
		}

		public void OptionsApply()
		{
			SetOSC(false);
		}

		private void SetOSC(bool canSaveConfig)
		{
			bool saveConfig = false;

			// OSC IP
			if (OSCIPText == "localhost") OSCIPText = "127.0.0.1";
			if (MyRegex.IP().IsMatch(OSCIPText) && IPAddress.TryParse(OSCIPText, out IPAddress? parsedIp) && !parsedIp.Equals(ConfigData.OSCIP))
			{
				ConfigData.OSCIP = parsedIp;
				saveConfig = true;
				if (_optionsViewModel != null) _optionsViewModel.RestartToApplyOptions = true;
			}

			// OSC Port
			if (Int32.TryParse(OSCPortText, out int parsedPort) && parsedPort <= 65535 && parsedPort > 0 && parsedPort != ConfigData.OSCPort)
			{
				ConfigData.OSCPort = parsedPort;
				saveConfig = true;
				if (_optionsViewModel != null) _optionsViewModel.RestartToApplyOptions = true;
			}

			//OSC Path
			if (!OSCPathText.StartsWith('/')) OSCPathText = "/" + OSCPathText;
			if (!OSCPathText.EndsWith('/')) OSCPathText += "/";
			if (OSCPathText != ConfigData.OSCPath)
			{
				ConfigData.OSCPath = OSCPathText;
				saveConfig = true;
			}

			// Save
			if (saveConfig && canSaveConfig) ConfigData.SaveConfig();

			OSCIPText = ConfigData.OSCIP.ToString();
			OSCPortText = ConfigData.OSCPort.ToString();
			OSCPathText = ConfigData.OSCPath;
		}
	}
}