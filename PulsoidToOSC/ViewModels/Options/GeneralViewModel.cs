using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace PulsoidToOSC.ViewModels.Options
{
	internal class GeneralViewModel : ViewModelBase
	{
		private readonly OptionsViewModel _optionsViewModel;
		public string Title => Locale.GetText(Locale.Keys.OptionsWindow.General.Title);
		public string Icon => "\xE80F";


		private string _tokenText = string.Empty;
		private bool _tokenTextBoxInFocus = false;
		private PulsoidApi.TokenValidityStatus? _tokenValidity = null;
		private bool _autoStartCheckmark = false;
		private bool _startMinimizedCheckmark = false;
		private bool _minimizeToTrayCheckmark = false;
		private string _selectedLocale = string.Empty;

		public bool TokenTextBoxInFocus
		{
			get => _tokenTextBoxInFocus;
			set
			{ 
				_tokenTextBoxInFocus = value;

				if (!MyRegex.GUID().IsMatch(_tokenText) && TokenText != string.Empty && !_tokenTextBoxInFocus)
				{
					TokenText = ConfigData.PulsoidToken;
				}
				else
				{
					OnPropertyChanged(nameof(TokenTextBlock));
				}
			}
		}
		public string TokenText
		{
			get => _tokenText;
			set
			{
				_tokenText = MyRegex.NotTokenSymbol().Replace(value ?? string.Empty, string.Empty);
				OnPropertyChanged();
				OnPropertyChanged(nameof(TokenTextBlock));
			}
		}
		public string TokenTextBlock
		{
			get => _tokenTextBoxInFocus ? _tokenText : MyRegex.TokenSymbolToHide().Replace(_tokenText, "●");
		}

		public PulsoidApi.TokenValidityStatus? TokenValidity
		{
			get => _tokenValidity;
			set { _tokenValidity = value; OnPropertyChanged(nameof(TokenValidationIndicator)); OnPropertyChanged(nameof(TokenValidationValid)); OnPropertyChanged(nameof(TokenValidationInvalid)); }
		}
		public string TokenValidationIndicator
		{
			get => _tokenValidity == PulsoidApi.TokenValidityStatus.Unknown ? "Visible" : "Hidden";
		}
		public string TokenValidationValid
		{
			get => _tokenValidity == PulsoidApi.TokenValidityStatus.Valid ? "Visible" : "Hidden";
		}
		public string TokenValidationInvalid
		{
			get => _tokenValidity == PulsoidApi.TokenValidityStatus.Invalid ? "Visible" : "Hidden";
		}

		public bool AutoStartCheckmark
		{
			get => _autoStartCheckmark;
			set { _autoStartCheckmark = value; OnPropertyChanged(); ToggleAutoStart(); }
		}

		public bool StartMinimizedCheckmark
		{
			get => _startMinimizedCheckmark;
			set { _startMinimizedCheckmark = value; OnPropertyChanged(); ToggleStartMinimized(); }
		}

		public bool MinimizeToTrayCheckmark
		{
			get => _minimizeToTrayCheckmark;
			set { _minimizeToTrayCheckmark = value; OnPropertyChanged(); ToggleCloseToTray(); }
		}

		public string SelectedLocale
		{
			get => _selectedLocale;
			set { _selectedLocale = value; OnPropertyChanged(); }
		}

		public class Locale_Item : ViewModelBase
		{
			private string _localeCode = string.Empty;
			public string LocaleCode
			{
				get => _localeCode;
				set { _localeCode = value; }
			}
			public string LocaleName
			{
				get => _localeCode == string.Empty ? Locale.GetText(Locale.Keys.OptionsWindow.General.Locale_SameAsSystem) : Locale.LocaleDefinitions[_localeCode].LocaleName;
			}
			public void RefreshLocaleName()
			{
				OnPropertyChanged(nameof(LocaleName));
			}
		}

		public ObservableCollection<Locale_Item> Locale_Items { get; set; } = new ObservableCollection<Locale_Item>(GetLocaleItems());
		private static List<Locale_Item> GetLocaleItems()
		{
			List<Locale_Item> items = [];
			foreach (string locale in Locale.LocaleDefinitions.Keys)
			{
				items.Add(new Locale_Item() { LocaleCode = locale });
			}

			items = items.OrderBy(f => f.LocaleName).ToList();
			// add system locale to the top of the list
			items.Insert(0, new Locale_Item() { LocaleCode = string.Empty });

			return items;
		}
			
		public Locale_Item Locale_SelectedItem
		{
			get => Locale_Items.FirstOrDefault(item => item.LocaleCode == _selectedLocale) ?? Locale_Items[0];
			set
			{
				_selectedLocale = value.LocaleCode;
				Locale.LoadCurrentLocale(_selectedLocale);
				ConfigData.Locale = _selectedLocale;
				ConfigData.SaveConfig();

				OnPropertyChanged();
				((LocaleUI)Application.Current.Resources["Locale"]).RefreshLocales();
				MainProgram.MainViewModel.RefreshLocale();
				_optionsViewModel.RefreshLocale();
			}
		}



		public ICommand GetTokenCommand { get; }
		public ICommand SetTokenCommand { get; }

		public GeneralViewModel(OptionsViewModel optionsViewModel)
		{
			_optionsViewModel = optionsViewModel;
			GetTokenCommand = new RelayCommand(GetToken);
			SetTokenCommand = new RelayCommand(SetToken);
		}

		private void GetToken()
		{
			_ = PulsoidApi.GetPulsoidToken_DeviceAuthorizationFlow();
		}

		private void SetToken() { SetToken(true, true); }
		private async void SetToken(bool canSaveConfig = true, bool validate = true)
		{
			(_optionsViewModel.OptionsWindow?.FindName("SetTokenButton") as UIElement)?.Focus();

			TokenValidity = PulsoidApi.TokenValidityStatus.Unknown;

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

		private void ToggleStartMinimized()
		{
			if (ConfigData.StartMinimized == StartMinimizedCheckmark) return;
			ConfigData.StartMinimized = StartMinimizedCheckmark;
			ConfigData.SaveConfig();
		}

		private void ToggleCloseToTray()
		{
			if (ConfigData.MinimizeToTray == MinimizeToTrayCheckmark) return;
			ConfigData.MinimizeToTray = MinimizeToTrayCheckmark;
			ConfigData.SaveConfig();
		}

		public void OptionsApply(bool done = false)
		{
			SetToken(false, !done);
		}

		public void RefreshLocale()
		{
			OnPropertyChanged(nameof(Title));

			Locale_Items.FirstOrDefault(item => item.LocaleCode == string.Empty)?.RefreshLocaleName();
		}
	}
}
