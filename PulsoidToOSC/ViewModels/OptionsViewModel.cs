using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace PulsoidToOSC
{
	internal class OptionsViewModel : ViewModelBase
	{
		private readonly MainViewModel _mainViewModel;
		public OptionsWindow? OptionsWindow { get; private set; }
		public bool RestartToApplyOptions { get; set; } = false;

		public OptionsGeneralViewModel OptionsGeneralViewModel { get; }
		public OptionsOscViewModel OptionsOscViewModel { get; }
		public OptionsVRChatViewModel OptionsVRChatViewModel { get; }
		public OptionsHeartrateViewModel OptionsHeartrateViewModel { get; }
		public OptionsParametersViewModel OptionsParametersViewModel { get; }
		public OptionsUIViewModel OptionsUIViewModel { get; }

		public ICommand OptionsDoneCommand { get; }
		public ICommand OptionsApplyCommand { get; }

		public OptionsViewModel(MainViewModel mainViewModel)
		{
			_mainViewModel = mainViewModel;
			OptionsGeneralViewModel = new OptionsGeneralViewModel(this);
			OptionsOscViewModel = new OptionsOscViewModel(this);
			OptionsVRChatViewModel = new OptionsVRChatViewModel(this);
			OptionsHeartrateViewModel = new OptionsHeartrateViewModel(this);
			OptionsParametersViewModel = new OptionsParametersViewModel(this);
			OptionsUIViewModel = new OptionsUIViewModel(this);

			OptionsDoneCommand = new RelayCommand(OptionsDone);
			OptionsApplyCommand = new RelayCommand(OptionsApply);
		}

		public void OpenOptionsWindow()
		{
			// General
			OptionsGeneralViewModel.TokenText = ConfigData.PulsoidToken;
			OptionsGeneralViewModel.TokenValidity = PulsoidApi.TokenValidity;
			if (PulsoidApi.TokenValidity == PulsoidApi.TokenValidities.Unknown)
			{
				Task.Run(async () =>
				{
					await PulsoidApi.ValidateToken();
					OptionsGeneralViewModel.TokenValidity = PulsoidApi.TokenValidity;
				});
			}
			OptionsGeneralViewModel.AutoStartCheckmark = ConfigData.AutoStart;

			// OSC
			OptionsOscViewModel.OSCManualConfigCheckmark = ConfigData.OSCUseManualConfig;
			OptionsOscViewModel.OSCIPText = ConfigData.OSCIP.ToString();
			OptionsOscViewModel.OSCPortText = ConfigData.OSCPort.ToString();
			OptionsOscViewModel.OSCPathText = ConfigData.OSCPath;

			// VRChat
			OptionsVRChatViewModel.VRCAutoConfigCheckmark = ConfigData.VRCUseAutoConfig;
			OptionsVRChatViewModel.VRCClinetsOnLANCheckmark = ConfigData.VRCSendToAllClinetsOnLAN;
			OptionsVRChatViewModel.VRCChatboxCheckmark = ConfigData.VRCSendBPMToChatbox;
			OptionsVRChatViewModel.VRCChatboxMessageText = ConfigData.VRCChatboxMessage;

			// Heart rate
			OptionsHeartrateViewModel.HrFloatMinText = ConfigData.HrFloatMin.ToString();
			OptionsHeartrateViewModel.HrFloatMaxText = ConfigData.HrFloatMax.ToString();
			OptionsHeartrateViewModel.HrTrendMinText = ConfigData.HrTrendMin.ToString(ConfigData.FloatLocal);
			OptionsHeartrateViewModel.HrTrendMaxText = ConfigData.HrTrendMax.ToString(ConfigData.FloatLocal);
			OptionsHeartrateViewModel.HrOffsetText = ConfigData.HrOffset.ToString();

			// UI
			OptionsUIViewModel.ColorErrorText = ConfigData.UIColorError;
			OptionsUIViewModel.ColorWarningText = ConfigData.UIColorWarning;
			OptionsUIViewModel.ColorRunningText = ConfigData.UIColorRunning;

			// Parameters
			OptionsParametersViewModel.Parameters.Clear();
			foreach (OSCParameter oscParameter in ConfigData.OSCParameters)
			{
				OptionsParametersViewModel.Parameters.Add(new() { ParametersOptionsViewModel = OptionsParametersViewModel, Name = oscParameter.Name, Type = oscParameter.Type });
			}

			// Open window
			OptionsWindow = new()
			{
				DataContext = this,
				Owner = _mainViewModel.MainWindow,
				WindowStartupLocation = WindowStartupLocation.CenterOwner
			};
			OptionsWindow.Closing += OptionsWindowClosing;
			OptionsWindow.ShowDialog();
		}

		public void OptionsApply()
		{
			(OptionsWindow?.FindName("ApplyOptionsButton") as UIElement)?.Focus();

			OptionsGeneralViewModel.OptionsApply();
			OptionsOscViewModel.OptionsApply();
			OptionsVRChatViewModel.OptionsApply();
			OptionsHeartrateViewModel.OptionsApply();
			OptionsUIViewModel.OptionsApply();
			OptionsParametersViewModel.OptionsApply();
			ConfigData.SaveConfig();
		}

		private void OptionsDone()
		{
			OptionsGeneralViewModel.OptionsApply(true);
			OptionsOscViewModel.OptionsApply();
			OptionsVRChatViewModel.OptionsApply();
			OptionsHeartrateViewModel.OptionsApply();
			OptionsUIViewModel.OptionsApply();
			OptionsParametersViewModel.OptionsApply();
			ConfigData.SaveConfig();

			OptionsWindow?.Close();
		}

		public void OptionsWindowClosing(object? sender, CancelEventArgs e)
		{
			PulsoidApi.StopGETServer();

			if (RestartToApplyOptions)
			{
				RestartToApplyOptions = false;
				MainProgram.RestartPulsoidToOSC();
			}
		}
	}
}