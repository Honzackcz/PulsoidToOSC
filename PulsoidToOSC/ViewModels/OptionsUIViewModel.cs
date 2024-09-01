using System.Windows.Input;
using System.Windows;

namespace PulsoidToOSC
{
	internal class OptionsUIViewModel : ViewModelBase
	{
		private readonly OptionsViewModel _optionsViewModel;

		private string _colorErrorText = string.Empty;
		private string _colorWarningText = string.Empty;
		private string _colorRunningText = string.Empty;

		public string ColorErrorText
		{
			get => _colorErrorText;
			set { _colorErrorText = "#" + (value?.Length > 7 ? _colorErrorText : MyRegex.NotHexCodeSymbol().Replace(value ?? string.Empty, string.Empty).ToUpper()).Replace("#", string.Empty); OnPropertyChanged(); OnPropertyChanged(nameof(ColorErrorIndicator)); }
		}
		public string ColorWarningText
		{
			get => _colorWarningText;
			set { _colorWarningText = "#" + (value?.Length > 7 ? _colorWarningText : MyRegex.NotHexCodeSymbol().Replace(value ?? string.Empty, string.Empty).ToUpper()).Replace("#", string.Empty); OnPropertyChanged(); OnPropertyChanged(nameof(ColorWarningIndicator)); }
		}
		public string ColorRunningText
		{
			get => _colorRunningText;
			set { _colorRunningText = "#" + (value?.Length > 7 ? _colorRunningText : MyRegex.NotHexCodeSymbol().Replace(value ?? string.Empty, string.Empty).ToUpper()).Replace("#", string.Empty); OnPropertyChanged(); OnPropertyChanged(nameof(ColorRunningIndicator)); }
		}

		public string ColorErrorIndicator
		{
			get => MyRegex.RGBHexCode().IsMatch(_colorErrorText) ? _colorErrorText : "#00000000";
		}
		public string ColorWarningIndicator
		{
			get => MyRegex.RGBHexCode().IsMatch(_colorWarningText) ? _colorWarningText : "#00000000";
		}
		public string ColorRunningIndicator
		{
			get => MyRegex.RGBHexCode().IsMatch(_colorRunningText) ? _colorRunningText : "#00000000";
		}

		public ICommand SetColorsCommand { get; }

		public OptionsUIViewModel(OptionsViewModel optionsViewModel)
		{
			_optionsViewModel = optionsViewModel;

			SetColorsCommand = new RelayCommand(SetColors);
		}

		private void SetColors()
		{
			(_optionsViewModel?.OptionsWindow?.FindName("SetColorsButton") as UIElement)?.Focus();

			bool saveConfig = false;

			if (MyRegex.RGBHexCode().IsMatch(ColorErrorText) && ColorErrorText != ConfigData.UIColorError)
			{
				ConfigData.UIColorError = ColorErrorText;
				saveConfig = true;
			}
			if (MyRegex.RGBHexCode().IsMatch(ColorWarningText) && ColorWarningText != ConfigData.UIColorWarning)
			{
				ConfigData.UIColorWarning = ColorWarningText;
				saveConfig = true;
			}
			if (MyRegex.RGBHexCode().IsMatch(ColorRunningText) && ColorRunningText != ConfigData.UIColorRunning)
			{
				ConfigData.UIColorRunning = ColorRunningText;
				saveConfig = true;
			}

			if (saveConfig) ConfigData.SaveConfig();

			ColorErrorText = ConfigData.UIColorError;
			ColorWarningText = ConfigData.UIColorWarning;
			ColorRunningText = ConfigData.UIColorRunning;
		}
	}
}
