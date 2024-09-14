using System.Windows.Input;
using System.Windows;
using System.Windows.Media;

namespace PulsoidToOSC
{
	internal class OptionsUIViewModel : ViewModelBase
	{
		private readonly OptionsViewModel _optionsViewModel;

		private ColorPickerWindow? ColorPickerWindow { get; set; }
		private enum ColorPickerEditingColor { None, Error, Warning, Running}
		private ColorPickerEditingColor _colorPickerEditingColor = ColorPickerEditingColor.None;

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

		public ICommand OpenColorPickerErrorCommand { get; }
		public ICommand OpenColorPickerWarningCommand { get; }
		public ICommand OpenColorPickerRunningCommand { get; }
		public ICommand ColorPickerDoneCommand { get; }
		public ICommand OptionsUIApplyCommand { get; }

		public OptionsUIViewModel(OptionsViewModel optionsViewModel)
		{
			_optionsViewModel = optionsViewModel;

			OpenColorPickerErrorCommand = new RelayCommand(OpenColorPickerError);
			OpenColorPickerWarningCommand = new RelayCommand(OpenColorPickerWarning);
			OpenColorPickerRunningCommand = new RelayCommand(OpenColorPickerRunning);
			ColorPickerDoneCommand = new RelayCommand(ColorPickerDone);
			OptionsUIApplyCommand = new RelayCommand(_optionsViewModel.OptionsApply);
		}

		public void OptionsApply()
		{
			SetColors(false);
		}

		private void SetColors(bool canSaveConfig)
		{
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

			if (saveConfig && canSaveConfig) ConfigData.SaveConfig();

			ColorErrorText = ConfigData.UIColorError;
			ColorWarningText = ConfigData.UIColorWarning;
			ColorRunningText = ConfigData.UIColorRunning;
		}

		private void OpenColorPickerError()
		{
			_colorPickerEditingColor = ColorPickerEditingColor.Error;
			OpenColorPicker(MyRegex.RGBHexCode().IsMatch(_colorErrorText) ? _colorErrorText : ConfigData.UIColorError);
		}

		private void OpenColorPickerWarning()
		{
			_colorPickerEditingColor = ColorPickerEditingColor.Warning;
			OpenColorPicker(MyRegex.RGBHexCode().IsMatch(_colorWarningText) ? _colorWarningText : ConfigData.UIColorWarning);
		}

		private void OpenColorPickerRunning()
		{
			_colorPickerEditingColor = ColorPickerEditingColor.Running;
			OpenColorPicker(MyRegex.RGBHexCode().IsMatch(_colorRunningText) ? _colorRunningText : ConfigData.UIColorRunning);
		}

		private void OpenColorPicker(string hexColor)
		{
			ColorPickerWindow = new()
			{
				DataContext = this,
				Owner = _optionsViewModel.OptionsWindow,
				WindowStartupLocation = WindowStartupLocation.CenterOwner
			};
			ColorPickerWindow.SetColor((Color)ColorConverter.ConvertFromString(hexColor));
			ColorPickerWindow.ShowDialog();
		}

		private void ColorPickerDone()
		{
			if (ColorPickerWindow == null) return;
			
			string hexColor = $"#{ColorPickerWindow.Color.R:X2}{ColorPickerWindow.Color.G:X2}{ColorPickerWindow.Color.B:X2}";

			switch (_colorPickerEditingColor)
			{
				case ColorPickerEditingColor.Error:
					ColorErrorText = hexColor;
					break;
				case ColorPickerEditingColor.Warning:
					ColorWarningText = hexColor;
					break;
				case ColorPickerEditingColor.Running:
					ColorRunningText = hexColor;
					break;
				default:
					break;
			}

			_colorPickerEditingColor = ColorPickerEditingColor.None;
			ColorPickerWindow.Close();
		}
	}
}