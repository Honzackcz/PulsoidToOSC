using PulsoidToOSC.ViewModels.Options;
using PulsoidToOSC.Windows;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace PulsoidToOSC.ViewModels
{
	internal class OptionsViewModel : ViewModelBase
	{
		private readonly MainViewModel _mainViewModel;
		public OptionsWindow? OptionsWindow { get; private set; }
		public bool RestartToApplyOptions { get; set; } = false;

		public GeneralViewModel GeneralViewModel { get; }
		public OSCViewModel OscViewModel { get; }
		public VRChatViewModel VRChatViewModel { get; }
		public HeartRateViewModel HeartRateViewModel { get; }
		public ParametersViewModel ParametersViewModel { get; }
		public UIViewModel UIViewModel { get; }
		public ToolsViewModel ToolsViewModel { get; }

		public ICommand OptionsDoneCommand { get; }
		public ICommand OptionsApplyCommand { get; }

		public OptionsViewModel(MainViewModel mainViewModel)
		{
			_mainViewModel = mainViewModel;
			GeneralViewModel = new GeneralViewModel(this);
			OscViewModel = new OSCViewModel(this);
			VRChatViewModel = new VRChatViewModel(this);
			HeartRateViewModel = new HeartRateViewModel(this);
			ParametersViewModel = new ParametersViewModel(this);
			UIViewModel = new UIViewModel(this);
			ToolsViewModel = new ToolsViewModel(this);

			OptionsDoneCommand = new RelayCommand(OptionsDone);
			OptionsApplyCommand = new RelayCommand(OptionsApply);

			OptionCategories =
			[
				GeneralViewModel,
				OscViewModel,
				VRChatViewModel,
				HeartRateViewModel,
				ParametersViewModel,
				UIViewModel,
				ToolsViewModel
			];

			SelectedCategory = OptionCategories.First();
		}

		public ObservableCollection<object> OptionCategories { get; }

		private object? _selectedCategory;
		public object? SelectedCategory
		{
			get => _selectedCategory;
			set
			{
				_selectedCategory = value;
				OnPropertyChanged();
			}
		}

		public void OpenOptionsWindow()
		{
			if (OptionsWindow != null)
			{
				OptionsWindow.Focus();
				return;
			}

			// General
			GeneralViewModel.TokenText = ConfigData.PulsoidToken;
			GeneralViewModel.TokenValidity = PulsoidApi.TokenValidity;
			if (PulsoidApi.TokenValidity == PulsoidApi.TokenValidityStatus.Unknown)
			{
				Task.Run(async () =>
				{
					await PulsoidApi.ValidateToken();
					GeneralViewModel.TokenValidity = PulsoidApi.TokenValidity;
				});
			}
			GeneralViewModel.AutoStartCheckmark = ConfigData.AutoStart;
			GeneralViewModel.StartMinimizedCheckmark = ConfigData.StartMinimized;
			GeneralViewModel.MinimizeToTrayCheckmark = ConfigData.MinimizeToTray;
			GeneralViewModel.SelectedLocale = Locale.CurrentLocale;

			// OSC
			OscViewModel.OSCManualConfigCheckmark = ConfigData.OSCUseManualConfig;
			OscViewModel.OSCIPText = ConfigData.OSCIP.ToString();
			OscViewModel.OSCPortText = ConfigData.OSCPort.ToString();
			OscViewModel.OSCPathText = ConfigData.OSCPath;

			// VRChat
			VRChatViewModel.VRCAutoConfigCheckmark = ConfigData.VRCUseAutoConfig;
			VRChatViewModel.VRCClinetsOnLANCheckmark = ConfigData.VRCSendToAllClinetsOnLAN;
			VRChatViewModel.VRCChatboxCheckmark = ConfigData.VRCSendBPMToChatbox;
			VRChatViewModel.VRCChatboxMessageText = ConfigData.VRCChatboxMessage;

			// Heart rate
			HeartRateViewModel.HrFloatMinText = ConfigData.HrFloatMin.ToString();
			HeartRateViewModel.HrFloatMaxText = ConfigData.HrFloatMax.ToString();
			HeartRateViewModel.HrTrendMinText = ConfigData.HrTrendMin.ToString(ConfigData.FloatLocale);
			HeartRateViewModel.HrTrendMaxText = ConfigData.HrTrendMax.ToString(ConfigData.FloatLocale);
			HeartRateViewModel.HrOffsetText = ConfigData.HrOffset.ToString();
			HeartRateViewModel.HrUndesiredValuesText = string.Join(";", ConfigData.HrUndesiredValues);
			HeartRateViewModel.HrRandomValueCheckmark = ConfigData.HrRandomValue;

			// Parameters
			ParametersViewModel.Parameters.Clear();
			foreach (OSCParameter oscParameter in ConfigData.OSCParameters)
			{
				ParametersViewModel.Parameters.Add(new() { ParametersOptionsViewModel = ParametersViewModel, Name = oscParameter.Name, Type = oscParameter.Type });
			}

			// UI
			UIViewModel.ColorUseCustomCheckmark = ConfigData.UIColorUseCustom;
			UIViewModel.ColorErrorText = ConfigData.UIColorError;
			UIViewModel.ColorWarningText = ConfigData.UIColorWarning;
			UIViewModel.ColorRunningText = ConfigData.UIColorRunning;

			// Tools
			ToolsViewModel.TestHeartRateButton = _mainViewModel.StartButton;


			// Open window
			OptionsWindow = new()
			{
				DataContext = this,
				Owner = _mainViewModel.MainWindow,
				
				WindowStartupLocation = _mainViewModel.MainWindow?.IsVisible ?? false ? WindowStartupLocation.CenterOwner : WindowStartupLocation.CenterScreen
			};
			OptionsWindow.Closing += OptionsWindowClosing;
			OptionsWindow.Show();
		}

		public void OptionsApply()
		{
			(OptionsWindow?.FindName("ApplyOptionsButton") as UIElement)?.Focus();

			GeneralViewModel.OptionsApply();
			OscViewModel.OptionsApply();
			VRChatViewModel.OptionsApply();
			HeartRateViewModel.OptionsApply();
			ParametersViewModel.OptionsApply();
			UIViewModel.OptionsApply();
			ToolsViewModel.OptionsApply();
			ConfigData.SaveConfig();
		}

		private void OptionsDone()
		{
			GeneralViewModel.OptionsApply(true);
			OscViewModel.OptionsApply();
			VRChatViewModel.OptionsApply();
			HeartRateViewModel.OptionsApply();
			ParametersViewModel.OptionsApply();
			UIViewModel.OptionsApply();
			ToolsViewModel.OptionsApply();
			ConfigData.SaveConfig();

			OptionsWindow?.Close();
		}

		public void OptionsWindowClosing(object? sender, CancelEventArgs e)
		{
			_ = PulsoidApi.CancelGetPulsoidToken_DeviceAuthorizationFlow();
			if (RestartToApplyOptions)
			{
				RestartToApplyOptions = false;
				MainProgram.RestartPulsoidToOSC();
			}
			OptionsWindow = null;
		}

		public void RefreshLocale()
		{
			GeneralViewModel.RefreshLocale();
			OscViewModel.RefreshLocale();
			VRChatViewModel.RefreshLocale();
			HeartRateViewModel.RefreshLocale();
			ParametersViewModel.RefreshLocale();
			UIViewModel.RefreshLocale();
			ToolsViewModel.RefreshLocale();
		}
	}
}