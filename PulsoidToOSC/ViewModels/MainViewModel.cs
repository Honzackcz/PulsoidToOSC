using System.Windows.Input;

namespace PulsoidToOSC
{
	internal class MainViewModel : ViewModelBase
	{
		public OptionsViewModel OptionsViewModel { get; }
		public InfoViewModel InfoViewModel { get; }
		public MainWindow? MainWindow { get; private set; }

		public enum StartButtonType { Disabled, Start, Stop }

		private StartButtonType _startButton = StartButtonType.Start;
		private readonly Dictionary<StartButtonType, string> StartButtonContents = new()
		{
			{StartButtonType.Disabled, string.Empty},
			{StartButtonType.Start, "Start"},
			{StartButtonType.Stop, "Stop"},
		};

		private string _bpmText = string.Empty;
		private string _measuredAtText = string.Empty;
		private string _infoText = string.Empty;
		private string _indicatorText = string.Empty;
		private string _textColor = "#00000000";

		public StartButtonType StartButton
		{
			get => _startButton;
			set { _startButton = value; OnPropertyChanged(nameof(StartButtonContent)); OnPropertyChanged(nameof(StartButtonEnabled)); }
		}
		public string StartButtonContent
		{
			get => StartButtonContents[_startButton];
		}
		public string StartButtonEnabled
		{
			get => _startButton == StartButtonType.Disabled ? "False" : "True";
		}

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
		public string InfoText
		{
			get => _infoText;
			set { _infoText = value; OnPropertyChanged(); }
		}
		public string IndicatorText
		{
			get => _indicatorText;
			set { _indicatorText = value; OnPropertyChanged(); }
		}
		public string TextColor
		{
			get => _textColor;
			set { _textColor = value; OnPropertyChanged(); }
		}

		public ICommand StartCommand { get; }
		public ICommand OpenOptionsCommand { get; }
		public ICommand OpenInfoCommand { get; }

		public MainViewModel()
		{
			OptionsViewModel = new OptionsViewModel(this);
			InfoViewModel = new InfoViewModel(this);

			StartCommand = new RelayCommand(Start);
			OpenOptionsCommand = new RelayCommand(OpenOptions);
			OpenInfoCommand = new RelayCommand(OpenInfo);
		}

		public void OpenMainWindow()
		{
			MainWindow = new()
			{
				DataContext = this
			};
			MainWindow.Show();
		}

		private void Start()
		{
			MainProgram.StartPulsoidToOSC();
		}

		private void OpenOptions()
		{
			OptionsViewModel.OpenOptionsWindow();
		}

		private void OpenInfo()
		{
			InfoViewModel.OpenInfoWindow();
		}

		public void SetError(string errorText)
		{
			TextColor = ConfigData.UIColorError;
			InfoText = errorText;
			BPMText = string.Empty;
			MeasuredAtText = string.Empty;
			IndicatorText = "\xEA39";
		}

		public void SetWarning(string warningText)
		{
			TextColor = ConfigData.UIColorWarning;
			InfoText = warningText;
			BPMText = string.Empty;
			MeasuredAtText = string.Empty;
			IndicatorText = "\xE7BA";
		}

		private int runningIndicator = 0;
		public void SetRunning(string bpmText, string measuredAtText)
		{
			TextColor = ConfigData.UIColorRunning;
			InfoText = string.Empty;
			BPMText = bpmText;
			MeasuredAtText = measuredAtText;
			IndicatorText = runningIndicator switch
			{
				0 => "\xE95E  \xE915  \xE915",
				1 => "\xE915  \xE95E  \xE915",
				2 => "\xE915  \xE915  \xE95E",
				_ => ""
			};
			runningIndicator++;
			if (runningIndicator > 2) runningIndicator = 0;
		}

		public void ClearUI()
		{
			TextColor = "#00000000";
			InfoText = string.Empty;
			BPMText = string.Empty;
			MeasuredAtText = string.Empty;
			IndicatorText = string.Empty;
			runningIndicator = 0;
		}
	}
}