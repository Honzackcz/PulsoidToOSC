using System.Windows.Input;
using System.Windows;

namespace PulsoidToOSC
{
	internal class OptionsHeartrateViewModel : ViewModelBase
	{
		private readonly OptionsViewModel _optionsViewModel;

		private string _hrFloatMinText = string.Empty;
		private string _hrFloatMaxText = string.Empty;
		private string _hrTrendMinText = string.Empty;
		private string _hrTrendMaxText = string.Empty;

		public string HrFloatMinText
		{
			get => _hrFloatMinText;
			set { _hrFloatMinText = MyRegex.NotNumber().Replace(value ?? string.Empty, string.Empty); OnPropertyChanged(); }
		}
		public string HrFloatMaxText
		{
			get => _hrFloatMaxText;
			set { _hrFloatMaxText = MyRegex.NotNumber().Replace(value ?? string.Empty, string.Empty); OnPropertyChanged(); }
		}
		public string HrTrendMinText
		{
			get => _hrTrendMinText;
			set { _hrTrendMinText = MyRegex.NotNumber().Replace(value ?? string.Empty, string.Empty); OnPropertyChanged(); }
		}
		public string HrTrendMaxText
		{
			get => _hrTrendMaxText;
			set { _hrTrendMaxText = MyRegex.NotNumber().Replace(value ?? string.Empty, string.Empty); OnPropertyChanged(); }
		}

		public ICommand SetHrFloatCommand { get; }
		public ICommand SetHrTrendCommand { get; }

		public OptionsHeartrateViewModel(OptionsViewModel optionsViewModel)
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
}
