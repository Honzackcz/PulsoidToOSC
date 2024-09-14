using System.Windows.Input;

namespace PulsoidToOSC
{
	internal class OptionsHeartrateViewModel : ViewModelBase
	{
		private readonly OptionsViewModel _optionsViewModel;

		private string _hrFloatMinText = string.Empty;
		private string _hrFloatMaxText = string.Empty;
		private string _hrTrendMinText = string.Empty;
		private string _hrTrendMaxText = string.Empty;
		private string _hrOffsetText = string.Empty;

		public string HrFloatMinText
		{
			get => _hrFloatMinText;
			set
			{
				string newText = value ?? string.Empty;

				if (newText == string.Empty)
				{
					_hrFloatMinText = newText;
				}
				else if (Int32.TryParse(newText, out int result))
				{
					_hrFloatMinText = Math.Clamp(result, 0, 255).ToString();
				}

				OnPropertyChanged();
			}
		}
		public string HrFloatMaxText
		{
			get => _hrFloatMaxText;
			set
			{
				string newText = value ?? string.Empty;

				if (newText == string.Empty)
				{
					_hrFloatMaxText = newText;
				}
				else if (Int32.TryParse(newText, out int result))
				{
					_hrFloatMaxText = Math.Clamp(result, 0, 255).ToString();
				}

				OnPropertyChanged();
			}
		}
		public string HrTrendMinText
		{
			get => _hrTrendMinText;
			set
			{
				string newText = value ?? string.Empty;
				newText = newText.EndsWith('.') ? newText.Replace(".", string.Empty) + "." : newText;

				if (newText == "." || newText == string.Empty)
				{
					_hrTrendMinText = newText;
				}
				else if (float.TryParse(newText, ConfigData.FloatStyle, ConfigData.FloatLocal, out float result))
				{
					if (newText.EndsWith('.') && result <= 255f)
					{
						_hrTrendMinText = newText;
					}
					else
					{
						_hrTrendMinText = Math.Clamp(result, 0.1f, 255f).ToString("0.##", ConfigData.FloatLocal);
					}
				}

				OnPropertyChanged();
			}
		}
		public string HrTrendMaxText
		{
			get => _hrTrendMaxText;
			set
			{
				string newText = value ?? string.Empty;
				newText = newText.EndsWith('.') ? newText.Replace(".", string.Empty) + "." : newText;

				if (newText == "." || newText == string.Empty)
				{
					_hrTrendMaxText = newText;
				}
				else if (float.TryParse(newText, ConfigData.FloatStyle, ConfigData.FloatLocal, out float result))
				{
					if (newText.EndsWith('.') && result <= 255f)
					{
						_hrTrendMaxText = newText;
					}
					else
					{
						_hrTrendMaxText = Math.Clamp(result, 0.1f, 255f).ToString("0.##", ConfigData.FloatLocal);
					}
				}

				OnPropertyChanged();
			}
		}
		public string HrOffsetText
		{
			get => _hrOffsetText;
			set
			{
				string newText = value ?? string.Empty;
				newText = newText.Contains('-') ? "-" + newText.Replace("-", string.Empty) : newText;

				if (newText == "-" || newText == string.Empty)
				{
					_hrOffsetText = newText;
				}
				else if (Int32.TryParse(newText, out int result))
				{
					_hrOffsetText = Math.Clamp(result, -254, 254).ToString();
				}

				OnPropertyChanged();
			}
		}

		public ICommand OptionsHrApplyCommand { get; }

		public OptionsHeartrateViewModel(OptionsViewModel optionsViewModel)
		{
			_optionsViewModel = optionsViewModel;

			OptionsHrApplyCommand = new RelayCommand(_optionsViewModel.OptionsApply);
		}

		public void OptionsApply()
		{
			SetHrFloat(false);
			SetHrTrend(false);
			SetOffset(false);
		}

		private void SetHrFloat(bool canSaveConfig)
		{
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

			if (saveConfig && canSaveConfig) ConfigData.SaveConfig();

			HrFloatMinText = ConfigData.HrFloatMin.ToString();
			HrFloatMaxText = ConfigData.HrFloatMax.ToString();
		}

		private void SetHrTrend(bool canSaveConfig)
		{
			bool saveConfig = false;

			if (float.TryParse(HrTrendMinText, ConfigData.FloatStyle, ConfigData.FloatLocal, out float parsedHrTrendMin) && parsedHrTrendMin <= 255f && parsedHrTrendMin >= 0.1f && parsedHrTrendMin != ConfigData.HrTrendMin)
			{
				ConfigData.HrTrendMin = parsedHrTrendMin;
				saveConfig = true;
			}

			if (float.TryParse(HrTrendMaxText, ConfigData.FloatStyle, ConfigData.FloatLocal, out float parsedHrTrendMax) && parsedHrTrendMax <= 255f && parsedHrTrendMax >= 0.1f && parsedHrTrendMax != ConfigData.HrTrendMax)
			{
				ConfigData.HrTrendMax = parsedHrTrendMax;
				saveConfig = true;
			}

			if (saveConfig && canSaveConfig) ConfigData.SaveConfig();

			HrTrendMinText = ConfigData.HrTrendMin.ToString(ConfigData.FloatLocal);
			HrTrendMaxText = ConfigData.HrTrendMax.ToString(ConfigData.FloatLocal);
		}

		private void SetOffset(bool canSaveConfig)
		{
			bool saveConfig = false;

			if (Int32.TryParse(HrOffsetText, out int parsedHrOffset) && parsedHrOffset < 255 && parsedHrOffset > -255 && parsedHrOffset != ConfigData.HrOffset)
			{
				ConfigData.HrOffset = parsedHrOffset;
				saveConfig = true;
			}

			if (saveConfig && canSaveConfig) ConfigData.SaveConfig();

			HrOffsetText = ConfigData.HrOffset.ToString();
		}
	}
}
