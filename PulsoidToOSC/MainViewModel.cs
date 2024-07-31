using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace PulsoidToOSC
{
	public class MainViewModel : INotifyPropertyChanged
	{
		//private readonly IAppService _appService;
		private bool restartToApplyOptions = false;

		public MainWindow? MainWindow { get; set; }
		private string _bpmText = string.Empty;
		private string _measuredAtText = string.Empty;
		private string _startButtonContent = "Start";
		private string _errorText = string.Empty;
		private string _errorTextColor = "#000000";
		private string _liveIndicatorColor = "#00000000";

		public OptionsWindow? OptionsWindow { get; set; }
		private string _tokenText = string.Empty;
		private bool _tokenValidationIndicator = false;
		private string _tokenValidationValid = "Hidden";
		private string _tokenValidationInvalid = "Hidden";
		private bool _autoStartCheckmark = false;

		private bool _oscManualConfigCheckmark = false;
		private string _oscIpText = string.Empty;
		private string _oscPortText = string.Empty;
		private string _oscPathText = string.Empty;

		private bool _vrcAutoConfigCheckmark = false;
		private bool _vrcClinetsOnLANCheckmark = false;
		private bool _vrcChatboxCheckmark = false;
		private string _vrcChatboxMessageText = string.Empty;

		public ObservableCollection<ParameterItem> Parameters { get; set; } = [];

		public class ParameterItem : INotifyPropertyChanged
		{
			public MainViewModel? MainViewModel { get; set; }
			private static readonly Dictionary<OSCParameter.Types, string> ParameterTypeNames = new()
			{
				{OSCParameter.Types.Integer, "Integer" },
				{OSCParameter.Types.Float, "Float [-1, 1]" },
				{OSCParameter.Types.Float01, "Float [0, 1]" },
				{OSCParameter.Types.BoolToggle, "Bool Toggle" }
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
			public ICommand DeleteParameterCommand { get; }

			public ParameterItem()
			{
				SetItegerTypeCommand = new RelayCommand(SetItegerType);
				SetFloatTypeCommand = new RelayCommand(SetFloatType);
				SetFloat01TypeCommand = new RelayCommand(SetFloat01Type);
				SetBoolToggleTypeCommand = new RelayCommand(SetBoolToggleType);
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
			private void DeleteParameter()
			{
				MainViewModel?.DeleteParameter(this);
			}

			public event PropertyChangedEventHandler? PropertyChanged;
			protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
			{
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		//MainWindow
		public string BPMText
		{
			get => _bpmText;
			set { _bpmText = value; OnPropertyChanged(); }
		}
		public string MeasuredAtText
		{
			get => _measuredAtText;
			set { _measuredAtText = value; OnPropertyChanged(); }
		}
		public string StartButtonContent
		{
			get => _startButtonContent;
			set { _startButtonContent = value; OnPropertyChanged(); }
		}
		public string ErrorText
		{
			get => _errorText;
			set { _errorText = value; OnPropertyChanged(); }
		}
		public string ErrorTextColor
		{
			get => _errorTextColor;
			set { _errorTextColor = value; OnPropertyChanged(); }
		}
		public string LiveIndicatorColor
		{
			get => _liveIndicatorColor;
			set { _liveIndicatorColor = value; OnPropertyChanged(); }
		}

		//OptionsWindow
		public string TokenText
		{
			get => _tokenText;
			set { _tokenText = MyRegex.NotTokenSymbol().Replace(value ?? string.Empty, ""); OnPropertyChanged(); OnPropertyChanged(nameof(TokenTextHidden)); }
		}
		public string TokenTextHidden
		{
			get => MyRegex.TokenSymbolToHide().Replace(_tokenText, "●");
		}

		public bool TokenValidationIndicator
		{
			get => _tokenValidationIndicator;
			set { _tokenValidationIndicator = value; OnPropertyChanged(); }
		}
		public string TokenValidationValid
		{
			get => _tokenValidationValid;
			set { _tokenValidationValid = value; OnPropertyChanged(); }
		}
		public string TokenValidationInvalid
		{
			get => _tokenValidationInvalid;
			set { _tokenValidationInvalid = value; OnPropertyChanged(); }
		}
		public bool AutoStartCheckmark
		{
			get => _autoStartCheckmark;
			set { _autoStartCheckmark = value; OnPropertyChanged(); ToggleAutoStart(); }
		}

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

		//MainWindow Commands
		public ICommand StartCommand { get; }
		public ICommand OpenOptionsCommand { get; }

		//OptionsWindow Commands
		public ICommand GetTokenCommand { get; }
		public ICommand SetTokenCommand { get; }

		public ICommand SetOSCIPCommand { get; }
		public ICommand SetOSCPortCommand { get; }
		public ICommand SetOSCPathCommand { get; }

		public ICommand SetVRCChatboxMessageCommand { get; }

		public ICommand AddNewParameterCommand { get; }
		public ICommand ApplyParametersCommand { get; }

		public ICommand OptionsDoneCommand { get; }

		public MainViewModel()
		{
			StartCommand = new RelayCommand(Start);
			OpenOptionsCommand = new RelayCommand(OpenOptions);

			GetTokenCommand = new RelayCommand(GetToken);
			SetTokenCommand = new RelayCommand(SetToken);

			SetOSCIPCommand = new RelayCommand(SetOSCIP);
			SetOSCPortCommand = new RelayCommand(SetOSCPort);
			SetOSCPathCommand = new RelayCommand(SetOSCPath);

			SetVRCChatboxMessageCommand = new RelayCommand(SetVRCChatboxMessage);

			AddNewParameterCommand = new RelayCommand(AddNewParameter);
			ApplyParametersCommand = new RelayCommand(ApplyParameters);

			OptionsDoneCommand = new RelayCommand(OptionsDone);
		}

		private void Start()
		{
			MainProgram.StartPulsoidToOSC();
		}
		private void OpenOptions()
		{
			TokenText = ConfigData.PulsoidToken;
			SetTokenValidationIndicator(PulsoidApi.TokenValiditi);
			if (PulsoidApi.TokenValiditi == PulsoidApi.TokenValidities.Unknown)
			{
				Task.Run(async () =>
				{
					await PulsoidApi.ValidateToken();
					SetTokenValidationIndicator(PulsoidApi.TokenValiditi);
				});
			}
			AutoStartCheckmark = ConfigData.AutoStart;

			OSCManualConfigCheckmark = ConfigData.OSCUseManualConfig;
			OSCIPText = ConfigData.OSCIP.ToString();
			OSCPortText = ConfigData.OSCPort.ToString();
			OSCPathText = ConfigData.OSCPath;

			VRCAutoConfigCheckmark = ConfigData.VRCUseAutoConfig;
			VRCClinetsOnLANCheckmark = ConfigData.VRCSendToAllClinetsOnLAN;
			VRCChatboxCheckmark = ConfigData.VRCSendBPMToChatbox;
			VRCChatboxMessageText = ConfigData.VRCChatboxMessage;

			Parameters.Clear();
			foreach (OSCParameter oscParameter in ConfigData.OSCParameters)
			{
				Parameters.Add(new() {MainViewModel = this, Name = oscParameter.Name, Type = oscParameter.Type});
			}

			OptionsWindow = new()
			{
				DataContext = this,
				Owner = MainWindow,
				WindowStartupLocation = WindowStartupLocation.CenterOwner
			};
			OptionsWindow.Closing += OptionsWindowClosing;
			OptionsWindow.ShowDialog();
		}

		private void GetToken()
		{
			PulsoidApi.GetPulsoidToken();
		}
		private async void SetToken()
		{
			(OptionsWindow?.FindName("SetTokenButton") as UIElement)?.Focus();
			
			SetTokenValidationIndicator(PulsoidApi.TokenValidities.Unknown);
			string previousToken = ConfigData.PulsoidToken;
			PulsoidApi.SetPulsoidToken(TokenText);
			TokenText = ConfigData.PulsoidToken;
			if (previousToken != ConfigData.PulsoidToken) restartToApplyOptions = true;

			await PulsoidApi.ValidateToken();
			await Task.Delay(250); //just for user to see the validation happens in case it was too fast
			SetTokenValidationIndicator(PulsoidApi.TokenValiditi);
		}

		private void ToggleAutoStart()
		{
			if (ConfigData.AutoStart == AutoStartCheckmark) return;
			ConfigData.AutoStart = AutoStartCheckmark;
			ConfigData.SaveConfig();
		}

		private void ToggleOSCManualConfig()
		{
			if (ConfigData.OSCUseManualConfig == OSCManualConfigCheckmark) return;
			ConfigData.OSCUseManualConfig = OSCManualConfigCheckmark;
			ConfigData.SaveConfig();
		}
		private void SetOSCIP()
		{
			(OptionsWindow?.FindName("SetOSCIPButton") as UIElement)?.Focus();

			IPAddress preiousIP = ConfigData.OSCIP;

			if (OSCIPText == "localhost") OSCIPText = "127.0.0.1";
			if (MyRegex.IP().IsMatch(OSCIPText) && IPAddress.TryParse(OSCIPText, out IPAddress? parsedIp) && parsedIp != ConfigData.OSCIP)
			{ 
				ConfigData.OSCIP = parsedIp;
				ConfigData.SaveConfig();
				restartToApplyOptions = true;
			}
			OSCIPText = ConfigData.OSCIP.ToString();
		}
		private void SetOSCPort()
		{
			(OptionsWindow?.FindName("SetOSCPortButton") as UIElement)?.Focus();

			if (Int32.TryParse(OSCPortText, out int parsedPort) && parsedPort <= 65535 && parsedPort > 0 && parsedPort != ConfigData.OSCPort)
			{
				ConfigData.OSCPort = parsedPort;
				ConfigData.SaveConfig();
				restartToApplyOptions = true;
			}
			OSCPortText = ConfigData.OSCPort.ToString();
		}
		private void SetOSCPath()
		{
			(OptionsWindow?.FindName("SetOSCPathButton") as UIElement)?.Focus();

			if (!OSCPathText.StartsWith('/')) OSCPathText =  "/" + OSCPathText;
			if (!OSCPathText.EndsWith('/')) OSCPathText += "/";
			if (ConfigData.OSCPath == OSCPathText) return;
			ConfigData.OSCPath = OSCPathText;
			ConfigData.SaveConfig();
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
			(OptionsWindow?.FindName("SetVRCChatboxMessageButton") as UIElement)?.Focus();

			if (ConfigData.VRCChatboxMessage == VRCChatboxMessageText) return;
			ConfigData.VRCChatboxMessage = VRCChatboxMessageText;
			ConfigData.SaveConfig();
		}

		private void OptionsDone()
		{
			if (OptionsWindow == null) return;
			OptionsWindow.Close();

			if (restartToApplyOptions)
			{
				restartToApplyOptions = false;
				MainProgram.RestartPulsoidToOSC();
			}
		}
		public void OptionsWindowClosing(object? sender, CancelEventArgs e)
		{
			if (restartToApplyOptions)
			{
				restartToApplyOptions = false;
				MainProgram.RestartPulsoidToOSC();
			}
		}

		private void DeleteParameter(ParameterItem parameterItem)
		{
			Parameters.Remove(parameterItem);
		}

		private void AddNewParameter()
		{
			Parameters.Add(new() { MainViewModel = this });
		}

		private void ApplyParameters()
		{
			List<OSCParameter> parameters = [];

			foreach (ParameterItem parameterItem in Parameters)
			{
				parameters.Add(new() { Name = parameterItem.Name, Type = parameterItem.Type});
			}

			ConfigData.OSCParameters = parameters;
			ConfigData.SaveConfig();
		}

		public event PropertyChangedEventHandler? PropertyChanged;
		protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		private void SetTokenValidationIndicator(PulsoidApi.TokenValidities? valid = null)
		{
			if (valid == null)
			{
				TokenValidationIndicator = false;
				TokenValidationInvalid = "Hidden";
				TokenValidationValid = "Hidden";
			}
			else if (valid == PulsoidApi.TokenValidities.Unknown)
			{
				TokenValidationIndicator = true;
				TokenValidationInvalid = "Hidden";
				TokenValidationValid = "Hidden";
			}
			else if (valid == PulsoidApi.TokenValidities.Invalid)
			{
				TokenValidationIndicator = false;
				TokenValidationInvalid = "Visible";
				TokenValidationValid = "Hidden";
			}
            else if (valid == PulsoidApi.TokenValidities.Valid)
            {
				TokenValidationIndicator = false;
				TokenValidationInvalid = "Hidden";
				TokenValidationValid = "Visible";
            }
        }
	}

	public class RelayCommand(Action execute, Func<bool>? canExecute = null) : ICommand
	{
		private readonly Action _execute = execute ?? throw new ArgumentNullException(nameof(execute));
		private readonly Func<bool>? _canExecute = canExecute;

		public bool CanExecute(object? parameter) => _canExecute == null || _canExecute();
		public void Execute(object? parameter) => _execute();
		public event EventHandler? CanExecuteChanged
		{
			add { CommandManager.RequerySuggested += value; }
			remove { CommandManager.RequerySuggested -= value; }
		}
	}
}