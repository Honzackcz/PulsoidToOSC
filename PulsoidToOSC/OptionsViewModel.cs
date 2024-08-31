using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net;
using System.Windows;
using System.Windows.Input;

namespace PulsoidToOSC
{
    internal class OptionsViewModel : ViewModelBase
    {
		private readonly MainViewModel _mainViewModel;
		public OptionsWindow? OptionsWindow { get; private set; }
		public bool RestartToApplyOptions { get; set; } = false;

		public GeneralOptionsViewModel GeneralOptionsViewModel { get; }
        public OscOptionsViewModel OscOptionsViewModel { get; }
        public VRChatOptionsViewModel VRChatOptionsViewModel { get; }
		public HeartrateOptionsViewModel HeartrateOptionsViewModel { get; }
        public ParametersOptionsViewModel ParametersOptionsViewModel { get; }

		public ICommand OptionsDoneCommand { get; }

		public OptionsViewModel(MainViewModel mainViewModel)
        {
			_mainViewModel = mainViewModel;
			GeneralOptionsViewModel = new GeneralOptionsViewModel(this);
			OscOptionsViewModel = new OscOptionsViewModel(this);
			VRChatOptionsViewModel = new VRChatOptionsViewModel(this);
			HeartrateOptionsViewModel = new HeartrateOptionsViewModel(this);
			ParametersOptionsViewModel = new ParametersOptionsViewModel(this);

			OptionsDoneCommand = new RelayCommand(OptionsDone);
		}

		public void OpenOptionsWindow()
		{
			GeneralOptionsViewModel.TokenText = ConfigData.PulsoidToken;
			GeneralOptionsViewModel.TokenValidity = PulsoidApi.TokenValidity;
			if (PulsoidApi.TokenValidity == PulsoidApi.TokenValidities.Unknown)
			{
				Task.Run(async () =>
				{
					await PulsoidApi.ValidateToken();
					GeneralOptionsViewModel.TokenValidity = PulsoidApi.TokenValidity;
				});
			}
			GeneralOptionsViewModel.AutoStartCheckmark = ConfigData.AutoStart;

			OscOptionsViewModel.OSCManualConfigCheckmark = ConfigData.OSCUseManualConfig;
			OscOptionsViewModel.OSCIPText = ConfigData.OSCIP.ToString();
			OscOptionsViewModel.OSCPortText = ConfigData.OSCPort.ToString();
			OscOptionsViewModel.OSCPathText = ConfigData.OSCPath;

			VRChatOptionsViewModel.VRCAutoConfigCheckmark = ConfigData.VRCUseAutoConfig;
			VRChatOptionsViewModel.VRCClinetsOnLANCheckmark = ConfigData.VRCSendToAllClinetsOnLAN;
			VRChatOptionsViewModel.VRCChatboxCheckmark = ConfigData.VRCSendBPMToChatbox;
			VRChatOptionsViewModel.VRCChatboxMessageText = ConfigData.VRCChatboxMessage;

			HeartrateOptionsViewModel.HrFloatMinText = ConfigData.HrFloatMin.ToString();
			HeartrateOptionsViewModel.HrFloatMaxText = ConfigData.HrFloatMax.ToString();
			HeartrateOptionsViewModel.HrTrendMinText = ConfigData.HrTrendMin.ToString();
			HeartrateOptionsViewModel.HrTrendMaxText = ConfigData.HrTrendMax.ToString();

			ParametersOptionsViewModel.Parameters.Clear();
			foreach (OSCParameter oscParameter in ConfigData.OSCParameters)
			{
				ParametersOptionsViewModel.Parameters.Add(new() { ParametersOptionsViewModel = ParametersOptionsViewModel, Name = oscParameter.Name, Type = oscParameter.Type });
			}

			OptionsWindow = new()
			{
				DataContext = this,
				Owner = _mainViewModel.MainWindow,
				WindowStartupLocation = WindowStartupLocation.CenterOwner
			};
			OptionsWindow.Closing += OptionsWindowClosing;
			OptionsWindow.ShowDialog();
		}

		private void OptionsDone()
		{
			if (OptionsWindow == null) return;
			OptionsWindow.Close();
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

    internal class GeneralOptionsViewModel : ViewModelBase
    {
		private readonly OptionsViewModel _optionsViewModel;

		private string _tokenText = string.Empty;
		private bool _autoStartCheckmark = false;
		private PulsoidApi.TokenValidities? _tokenValidity = null;

		public string TokenText
		{
			get => _tokenText;
			set { _tokenText = MyRegex.NotTokenSymbol().Replace(value ?? string.Empty, ""); OnPropertyChanged(); OnPropertyChanged(nameof(TokenTextHidden)); }
		}
		public string TokenTextHidden
		{
			get => MyRegex.TokenSymbolToHide().Replace(_tokenText, "●");
		}

		public PulsoidApi.TokenValidities? TokenValidity
		{
			get => _tokenValidity;
			set { _tokenValidity = value; OnPropertyChanged(nameof(TokenValidationIndicator)); OnPropertyChanged(nameof(TokenValidationValid)); OnPropertyChanged(nameof(TokenValidationInvalid)); }
		}
		public bool TokenValidationIndicator
		{
			get => _tokenValidity == PulsoidApi.TokenValidities.Unknown;
		}
		public string TokenValidationValid
		{
			get => _tokenValidity == PulsoidApi.TokenValidities.Valid ? "Visible" : "Hidden";
		}
		public string TokenValidationInvalid
		{
			get => _tokenValidity == PulsoidApi.TokenValidities.Invalid ? "Visible" : "Hidden";
		}

		public bool AutoStartCheckmark
		{
			get => _autoStartCheckmark;
			set { _autoStartCheckmark = value; OnPropertyChanged(); ToggleAutoStart(); }
		}


		public ICommand GetTokenCommand { get; }
		public ICommand SetTokenCommand { get; }

		public GeneralOptionsViewModel(OptionsViewModel optionsViewModel)
        {
			_optionsViewModel = optionsViewModel;
			GetTokenCommand = new RelayCommand(GetToken);
			SetTokenCommand = new RelayCommand(SetToken);
		}

		private void GetToken()
		{
			PulsoidApi.GetPulsoidToken();
		}

		private async void SetToken()
		{
			(_optionsViewModel?.OptionsWindow?.FindName("SetTokenButton") as UIElement)?.Focus();

			TokenValidity = PulsoidApi.TokenValidities.Unknown;
			string previousToken = ConfigData.PulsoidToken;
			PulsoidApi.SetPulsoidToken(TokenText);
			TokenText = ConfigData.PulsoidToken;
			if (previousToken != ConfigData.PulsoidToken && _optionsViewModel != null) _optionsViewModel.RestartToApplyOptions = true;

			await PulsoidApi.ValidateToken();
			await Task.Delay(250); //just for user to see the validation happens in case it was too fast
			TokenValidity = PulsoidApi.TokenValidity;
		}

		private void ToggleAutoStart()
		{
			if (ConfigData.AutoStart == AutoStartCheckmark) return;
			ConfigData.AutoStart = AutoStartCheckmark;
			ConfigData.SaveConfig();
		}
	}

    internal class OscOptionsViewModel : ViewModelBase
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
			set { _oscPortText = MyRegex.NotNumber().Replace(value ?? string.Empty, ""); OnPropertyChanged(); }
		}
		public string OSCPathText
		{
			get => _oscPathText;
			set { _oscPathText = value?.Replace("=", "") ?? string.Empty; OnPropertyChanged(); }
		}

		public ICommand SetOSCIPCommand { get; }
		public ICommand SetOSCPortCommand { get; }
		public ICommand SetOSCPathCommand { get; }

		public OscOptionsViewModel(OptionsViewModel optionsViewModel)
		{
			_optionsViewModel = optionsViewModel;
			SetOSCIPCommand = new RelayCommand(SetOSCIP);
			SetOSCPortCommand = new RelayCommand(SetOSCPort);
			SetOSCPathCommand = new RelayCommand(SetOSCPath);
		}

		private void ToggleOSCManualConfig()
		{
			if (ConfigData.OSCUseManualConfig == OSCManualConfigCheckmark) return;
			ConfigData.OSCUseManualConfig = OSCManualConfigCheckmark;
			ConfigData.SaveConfig();
		}

		private void SetOSCIP()
		{
			(_optionsViewModel?.OptionsWindow?.FindName("SetOSCIPButton") as UIElement)?.Focus();

			if (OSCIPText == "localhost") OSCIPText = "127.0.0.1";
			if (MyRegex.IP().IsMatch(OSCIPText) && IPAddress.TryParse(OSCIPText, out IPAddress? parsedIp) && parsedIp != ConfigData.OSCIP)
			{
				ConfigData.OSCIP = parsedIp;
				ConfigData.SaveConfig();
				if (_optionsViewModel != null) _optionsViewModel.RestartToApplyOptions = true;
			}
			OSCIPText = ConfigData.OSCIP.ToString();
		}

		private void SetOSCPort()
		{
			(_optionsViewModel?.OptionsWindow?.FindName("SetOSCPortButton") as UIElement)?.Focus();

			if (Int32.TryParse(OSCPortText, out int parsedPort) && parsedPort <= 65535 && parsedPort > 0 && parsedPort != ConfigData.OSCPort)
			{
				ConfigData.OSCPort = parsedPort;
				ConfigData.SaveConfig();
				if (_optionsViewModel != null) _optionsViewModel.RestartToApplyOptions = true;
			}
			OSCPortText = ConfigData.OSCPort.ToString();
		}

		private void SetOSCPath()
		{
			(_optionsViewModel?.OptionsWindow?.FindName("SetOSCPathButton") as UIElement)?.Focus();

			if (!OSCPathText.StartsWith('/')) OSCPathText = "/" + OSCPathText;
			if (!OSCPathText.EndsWith('/')) OSCPathText += "/";
			if (ConfigData.OSCPath == OSCPathText) return;
			ConfigData.OSCPath = OSCPathText;
			ConfigData.SaveConfig();
		}
	}

	internal class VRChatOptionsViewModel : ViewModelBase
    {
		private readonly OptionsViewModel _optionsViewModel;

		private bool _vrcAutoConfigCheckmark = false;
		private bool _vrcClinetsOnLANCheckmark = false;
		private bool _vrcChatboxCheckmark = false;
		private string _vrcChatboxMessageText = string.Empty;

		public bool VRCAutoConfigCheckmark
		{
			get => _vrcAutoConfigCheckmark;
			set { _vrcAutoConfigCheckmark = value; OnPropertyChanged(); ToggleVRCAutoConfig(); }
		}
		public bool VRCClinetsOnLANCheckmark
		{
			get => _vrcClinetsOnLANCheckmark;
			set { _vrcClinetsOnLANCheckmark = value; OnPropertyChanged(); ToggleVRCClinetsOnLAN(); }
		}
		public bool VRCChatboxCheckmark
		{
			get => _vrcChatboxCheckmark;
			set { _vrcChatboxCheckmark = value; OnPropertyChanged(); ToggleVRCChatbox(); }
		}
		public string VRCChatboxMessageText
		{
			get => _vrcChatboxMessageText;
			set { _vrcChatboxMessageText = value?.Replace("=", "") ?? string.Empty; OnPropertyChanged(); }
		}

		public ICommand SetVRCChatboxMessageCommand { get; }

		public VRChatOptionsViewModel(OptionsViewModel optionsViewModel)
		{
			_optionsViewModel = optionsViewModel;
			SetVRCChatboxMessageCommand = new RelayCommand(SetVRCChatboxMessage);
		}

		private void ToggleVRCAutoConfig()
		{
			if (ConfigData.VRCUseAutoConfig == VRCAutoConfigCheckmark) return;
			ConfigData.VRCUseAutoConfig = VRCAutoConfigCheckmark;
			ConfigData.SaveConfig();
		}

		private void ToggleVRCClinetsOnLAN()
		{
			if (ConfigData.VRCSendToAllClinetsOnLAN == VRCClinetsOnLANCheckmark) return;
			ConfigData.VRCSendToAllClinetsOnLAN = VRCClinetsOnLANCheckmark;
			ConfigData.SaveConfig();
		}

		private void ToggleVRCChatbox()
		{
			if (ConfigData.VRCSendBPMToChatbox == VRCChatboxCheckmark) return;
			ConfigData.VRCSendBPMToChatbox = VRCChatboxCheckmark;
			ConfigData.SaveConfig();
		}

		private void SetVRCChatboxMessage()
		{
			(_optionsViewModel?.OptionsWindow?.FindName("SetVRCChatboxMessageButton") as UIElement)?.Focus();

			if (ConfigData.VRCChatboxMessage == VRCChatboxMessageText) return;
			ConfigData.VRCChatboxMessage = VRCChatboxMessageText;
			ConfigData.SaveConfig();
		}
	}

	internal class HeartrateOptionsViewModel : ViewModelBase
	{
		private readonly OptionsViewModel _optionsViewModel;

		private string _hrFloatMinText = string.Empty;
		private string _hrFloatMaxText = string.Empty;
		private string _hrTrendMinText = string.Empty;
		private string _hrTrendMaxText = string.Empty;

		public string HrFloatMinText
		{
			get => _hrFloatMinText;
			set { _hrFloatMinText = MyRegex.NotNumber().Replace(value ?? string.Empty, ""); OnPropertyChanged(); }
		}
		public string HrFloatMaxText
		{
			get => _hrFloatMaxText;
			set { _hrFloatMaxText = MyRegex.NotNumber().Replace(value ?? string.Empty, ""); OnPropertyChanged(); }
		}
		public string HrTrendMinText
		{
			get => _hrTrendMinText;
			set { _hrTrendMinText = MyRegex.NotNumber().Replace(value ?? string.Empty, ""); OnPropertyChanged(); }
		}
		public string HrTrendMaxText
		{
			get => _hrTrendMaxText;
			set { _hrTrendMaxText = MyRegex.NotNumber().Replace(value ?? string.Empty, ""); OnPropertyChanged(); }
		}

		public ICommand SetHrFloatCommand { get; }
		public ICommand SetHrTrendCommand { get; }

		public HeartrateOptionsViewModel(OptionsViewModel optionsViewModel)
		{
			_optionsViewModel = optionsViewModel;

			SetHrFloatCommand = new RelayCommand(SetHrFloat);
			SetHrTrendCommand = new RelayCommand(SetHrTrend);
		}

		private void SetHrFloat()
		{
			(_optionsViewModel?.OptionsWindow?.FindName("SetHrFloatButton") as UIElement)?.Focus();

			bool saveConfig = false;

			if (Int32.TryParse(HrFloatMinText, out int parsedHrFloatMin) && parsedHrFloatMin <= 255 && parsedHrFloatMin >= 0 && parsedHrFloatMin != ConfigData.HrFloatMin)
			{
				ConfigData.HrFloatMin = parsedHrFloatMin;
				saveConfig = true;
			}

			if (Int32.TryParse(HrFloatMaxText, out int parsedHrFloatMax) && parsedHrFloatMax <= 255 && parsedHrFloatMax >= 0 && parsedHrFloatMax != ConfigData.HrFloatMax)
			{
				ConfigData.HrFloatMax = parsedHrFloatMax;
				saveConfig = true;
			}

			if (saveConfig) ConfigData.SaveConfig();

			HrFloatMinText = ConfigData.HrFloatMin.ToString();
			HrFloatMaxText = ConfigData.HrFloatMax.ToString();
		}

		private void SetHrTrend()
		{
			(_optionsViewModel?.OptionsWindow?.FindName("SetHrTrendButton") as UIElement)?.Focus();

			bool saveConfig = false;

			if (Int32.TryParse(HrTrendMinText, out int parsedHrTrendMin) && parsedHrTrendMin <= 65535 && parsedHrTrendMin > 0 && parsedHrTrendMin != ConfigData.HrTrendMin)
			{
				ConfigData.HrTrendMin = parsedHrTrendMin;
				saveConfig = true;
			}

			if (Int32.TryParse(HrTrendMaxText, out int parsedHrTrendMax) && parsedHrTrendMax <= 65535 && parsedHrTrendMax > 0 && parsedHrTrendMax != ConfigData.HrTrendMax)
			{
				ConfigData.HrTrendMax = parsedHrTrendMax;
				saveConfig = true;
			}

			if (saveConfig) ConfigData.SaveConfig();

			HrTrendMinText = ConfigData.HrTrendMin.ToString();
			HrTrendMaxText = ConfigData.HrTrendMax.ToString();
		}
	}

	internal class ParametersOptionsViewModel : ViewModelBase
    {
		private readonly OptionsViewModel _optionsViewModel;

		public ObservableCollection<ParameterItem> Parameters { get; set; } = [];

		public class ParameterItem : ViewModelBase
		{
			public ParametersOptionsViewModel? ParametersOptionsViewModel { get; set; }
			private static readonly Dictionary<OSCParameter.Types, string> ParameterTypeNames = new()
			{
				{OSCParameter.Types.Integer, "Integer" },
				{OSCParameter.Types.Float, "Float [-1, 1]" },
				{OSCParameter.Types.Float01, "Float [0, 1]" },
				{OSCParameter.Types.BoolToggle, "Bool Toggle" },
				{OSCParameter.Types.BoolActive, "Bool Active" },
				{OSCParameter.Types.TrendF, "Trend [-1, 1]" },
				{OSCParameter.Types.TrendF01, "Trend [0, 1]" }
			};

			private OSCParameter.Types _type = OSCParameter.Types.Integer;
			private string _name = string.Empty;

			public OSCParameter.Types Type
			{
				get => _type;
				set { _type = value; OnPropertyChanged(nameof(TypeName)); }
			}

			public string Name
			{
				get => _name;
				set { _name = value?.Replace("=", "").Replace(";", "") ?? string.Empty; OnPropertyChanged(); }
			}
			public string TypeName { get => ParameterTypeNames[_type]; }

			public ICommand SetItegerTypeCommand { get; }
			public ICommand SetFloatTypeCommand { get; }
			public ICommand SetFloat01TypeCommand { get; }
			public ICommand SetBoolToggleTypeCommand { get; }
			public ICommand SetBoolActiveTypeCommand { get; }
			public ICommand SetTrendFTypeCommand { get; }
			public ICommand SetTrendF01TypeCommand { get; }
			public ICommand DeleteParameterCommand { get; }

			public ParameterItem()
			{
				SetItegerTypeCommand = new RelayCommand(SetItegerType);
				SetFloatTypeCommand = new RelayCommand(SetFloatType);
				SetFloat01TypeCommand = new RelayCommand(SetFloat01Type);
				SetBoolToggleTypeCommand = new RelayCommand(SetBoolToggleType);
				SetBoolActiveTypeCommand = new RelayCommand(SetBoolActiveType);
				SetTrendFTypeCommand = new RelayCommand(SetTrendFType);
				SetTrendF01TypeCommand = new RelayCommand(SetTrendF01Type);
				DeleteParameterCommand = new RelayCommand(DeleteParameter);
			}

			private void SetItegerType()
			{
				Type = OSCParameter.Types.Integer;
			}

			private void SetFloatType()
			{
				Type = OSCParameter.Types.Float;
			}

			private void SetFloat01Type()
			{
				Type = OSCParameter.Types.Float01;
			}

			private void SetBoolToggleType()
			{
				Type = OSCParameter.Types.BoolToggle;
			}

			private void SetBoolActiveType()
			{
				Type = OSCParameter.Types.BoolActive;
			}

			private void SetTrendFType()
			{
				Type = OSCParameter.Types.TrendF;
			}

			private void SetTrendF01Type()
			{
				Type = OSCParameter.Types.TrendF01;
			}

			private void DeleteParameter()
			{
				ParametersOptionsViewModel?.DeleteParameter(this);
			}
		}

		public ICommand AddNewParameterCommand { get; }
		public ICommand ApplyParametersCommand { get; }

		public ParametersOptionsViewModel(OptionsViewModel optionsViewModel)
		{
			_optionsViewModel = optionsViewModel;
			AddNewParameterCommand = new RelayCommand(AddNewParameter);
			ApplyParametersCommand = new RelayCommand(ApplyParameters);
		}

		private void DeleteParameter(ParameterItem parameterItem)
		{
			Parameters.Remove(parameterItem);
		}

		private void AddNewParameter()
		{
			Parameters.Add(new() { ParametersOptionsViewModel = this });
		}

		private void ApplyParameters()
		{
			List<OSCParameter> parameters = [];

			foreach (ParameterItem parameterItem in Parameters)
			{
				parameters.Add(new() { Name = parameterItem.Name, Type = parameterItem.Type });
			}

			ConfigData.OSCParameters = parameters;
			ConfigData.SaveConfig();
		}
	}
}