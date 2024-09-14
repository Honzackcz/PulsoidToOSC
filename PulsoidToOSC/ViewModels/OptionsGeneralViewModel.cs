using System.Windows.Input;
using System.Windows;

namespace PulsoidToOSC
{
	internal class OptionsGeneralViewModel : ViewModelBase
	{
		private readonly OptionsViewModel _optionsViewModel;

		private string _tokenText = string.Empty;
		private bool _autoStartCheckmark = false;
		private PulsoidApi.TokenValidities? _tokenValidity = null;

		public string TokenText
		{
			get => _tokenText;
			set { _tokenText = MyRegex.NotTokenSymbol().Replace(value ?? string.Empty, string.Empty); OnPropertyChanged(); OnPropertyChanged(nameof(TokenTextHidden)); }
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

		public OptionsGeneralViewModel(OptionsViewModel optionsViewModel)
		{
			_optionsViewModel = optionsViewModel;
			GetTokenCommand = new RelayCommand(GetToken);
			SetTokenCommand = new RelayCommand(SetToken);
		}

		private void GetToken()
		{
			PulsoidApi.GetPulsoidToken();
		}

		private void SetToken() { SetToken(true, true); }
		private async void SetToken(bool canSaveConfig = true, bool validate = true)
		{
			(_optionsViewModel?.OptionsWindow?.FindName("SetTokenButton") as UIElement)?.Focus();

			TokenValidity = PulsoidApi.TokenValidities.Unknown;

			if (TokenText != ConfigData.PulsoidToken)
			{
				string previousToken = ConfigData.PulsoidToken;
				PulsoidApi.SetPulsoidToken(TokenText, canSaveConfig);
				TokenText = ConfigData.PulsoidToken;
				if (previousToken != ConfigData.PulsoidToken && _optionsViewModel != null) _optionsViewModel.RestartToApplyOptions = true;
			}

			if (validate)
			{
				await PulsoidApi.ValidateToken();
				await Task.Delay(250); //just for user to see the validation happens in case it was too fast
				TokenValidity = PulsoidApi.TokenValidity;
			}
		}

		private void ToggleAutoStart()
		{
			if (ConfigData.AutoStart == AutoStartCheckmark) return;
			ConfigData.AutoStart = AutoStartCheckmark;
			ConfigData.SaveConfig();
		}

		public void OptionsApply(bool done = false)
		{
			SetToken(false, !done);
		}
	}
}
