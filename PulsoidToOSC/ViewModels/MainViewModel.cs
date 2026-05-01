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

		private readonly string[] indicatorsRunning = ["\xE95E  \xE915  \xE915", "\xE915  \xE95E  \xE915", "\xE915  \xE915  \xE95E"];
		private readonly string[] indicatorsTesting = ["\xEC7A  \xE915  \xE915", "\xE915  \xEC7A  \xE915", "\xE915  \xE915  \xEC7A"];
		private int indicatorState = 0;

		private string _bpmText = string.Empty;
		private string _measuredAtText = string.Empty;
		private string _infoText = string.Empty;
		private string _indicatorText = string.Empty;
		private string _textColorType = string.Empty;
		private string _textColor = "#00000000";
		private bool _trayIconVisible = false;

		public StartButtonType StartButton
		{
			get => _startButton;
			set { 
				_startButton = value;
				OptionsViewModel.OptionsToolslViewModel.TestHeartRateButton = _startButton;
				OnPropertyChanged(nameof(StartButtonContent));
				OnPropertyChanged(nameof(StartButtonEnabled));
			}
		}
		public string StartButtonContent
		{
			get => StartButtonContents(_startButton);
		}
		public bool StartButtonEnabled
		{
			get => _startButton != StartButtonType.Disabled;
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
		public string TextColorType
		{
			get => _textColorType;
			set { _textColorType = value; OnPropertyChanged(); }
		}
		public string TextColor
		{
			get => _textColor;
			set { _textColor = value; OnPropertyChanged(); }
		}

		public string TrayIconVisibility
		{
			get => _trayIconVisible ? "Visible" : "Hidden";
		}
		public bool TrayIconVisible
		{
			get => _trayIconVisible;
			set { _trayIconVisible = value; OnPropertyChanged(); OnPropertyChanged(nameof(TrayIconVisibility)); }
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

			if (ConfigData.StartMinimized && ConfigData.AutoStart)
			{
				if (ConfigData.MinimizeToTray)
				{
					MainWindow.Hide();
					MainProgram.MainViewModel.TrayIconVisible = true;
				}
				else
				{
					MainWindow.WindowState = WindowState.Minimized;
			MainWindow.Show();
		}
			}
			else
			{
				MainWindow.Show();
			}
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
			if (ConfigData.UIColorUseCustom) TextColorType = "Custom";
			else TextColorType = "Error";
			TextColor = ConfigData.UIColorError;
			InfoText = errorText;
			BPMText = string.Empty;
			MeasuredAtText = string.Empty;
			IndicatorText = "\xEA39";
		}

		public void SetWarning(string warningText)
		{
			if (ConfigData.UIColorUseCustom) TextColorType = "Custom";
			else TextColorType = "Warning";
			TextColor = ConfigData.UIColorWarning;
			InfoText = warningText;
			BPMText = string.Empty;
			MeasuredAtText = string.Empty;
			IndicatorText = "\xE7BA";
		}

		public void SetRunning(string bpmText, string measuredAtText, bool testing = false)
		{
			if (ConfigData.UIColorUseCustom) TextColorType = "Custom";
			else TextColorType = "Running";
			TextColor = ConfigData.UIColorRunning;
			InfoText = string.Empty;
			BPMText = bpmText;
			MeasuredAtText = measuredAtText;
			IndicatorText = testing ? indicatorsTesting[indicatorState] : indicatorsRunning[indicatorState];
			indicatorState++;
			if (indicatorState > 2) indicatorState = 0;
		}

		public void ClearUI()
		{
			TextColorType = string.Empty;
			TextColor = "#00000000";
			InfoText = string.Empty;
			BPMText = string.Empty;
			MeasuredAtText = string.Empty;
			IndicatorText = string.Empty;
			indicatorState = 0;
		}

		private static string StartButtonContents(StartButtonType type)
		{
			return type switch
			{
				StartButtonType.Disabled => string.Empty,
				StartButtonType.Start => Locale.GetText(Locale.Keys.General.Start),
				StartButtonType.Stop => Locale.GetText(Locale.Keys.General.Stop),
				_ => string.Empty,
			};
		}

		public enum StatusType { None, Running, RunningTest, AutoStart, TestStart, Connecting, Waiting, Closing, InvalidToken, ConnectionError, ConnectionRetry, DataError }
		private StatusType _currentStatus = StatusType.None;
		private int _currentBpm = 0;
		private DateTime _currentMeasuredAt = DateTime.MinValue;
		private int _currentRetryAttempt = 0;
		private void SetStatus(StatusType status, int bpm, DateTime measuredAt, int retryAttempt)
		{
			_currentStatus = status;
			_currentBpm = bpm;
			_currentMeasuredAt = measuredAt;
			_currentRetryAttempt = retryAttempt;

			switch (status)
			{
				//running
				case StatusType.Running:
					SetRunning(Locale.GetText(Locale.Keys.MainWindow.Status.BPM, bpm), measuredAt > DateTime.MinValue ? Locale.GetText(Locale.Keys.MainWindow.Status.MeasuredAt, measuredAt.ToLongTimeString()) : string.Empty, false);
					break;
				case StatusType.RunningTest:
					SetRunning(Locale.GetText(Locale.Keys.MainWindow.Status.BPM, bpm), measuredAt > DateTime.MinValue ? Locale.GetText(Locale.Keys.MainWindow.Status.MeasuredAt, measuredAt.ToLongTimeString()) : string.Empty, true);
					break;

				//warning
				case StatusType.AutoStart:
					SetWarning(Locale.GetText(Locale.Keys.MainWindow.Status.AutoStart));
					break;
				case StatusType.TestStart:
					SetWarning(Locale.GetText(Locale.Keys.MainWindow.Status.TestStart));
					break;
				case StatusType.Connecting:
					SetWarning(Locale.GetText(Locale.Keys.MainWindow.Status.Connecting));
					break;
				case StatusType.Waiting:
					SetWarning(Locale.GetText(Locale.Keys.MainWindow.Status.Waiting));
					break;
				case StatusType.Closing:
					SetWarning(Locale.GetText(Locale.Keys.MainWindow.Status.Closing));
					break;

				//error
				case StatusType.InvalidToken:
					SetError(Locale.GetText(Locale.Keys.MainWindow.Status.InvalidToken));
					break;
				case StatusType.ConnectionError:
					SetError(Locale.GetText(Locale.Keys.MainWindow.Status.ConnectionError));
					break;
				case StatusType.ConnectionRetry:
					SetError(Locale.GetText(Locale.Keys.MainWindow.Status.RetryConnection, retryAttempt));
					break;
				case StatusType.DataError:
					SetError(Locale.GetText(Locale.Keys.MainWindow.Status.DataError));
					break;

				default:
					ClearUI();
					return;
			}
		}
		public void SetStatus(StatusType status)
		{
			SetStatus(status, 0, DateTime.MinValue, 0);
		}
		public void SetStatus(StatusType status, int bpm, DateTime measuredAt)
		{
			SetStatus(status, bpm, measuredAt, 0);
		}
		public void SetStatus(StatusType status, int retryAttempt)
		{
			SetStatus(status, 0, DateTime.MinValue, retryAttempt);
		}
		public void RefreshLocale()
		{
			StartButton = _startButton;
			SetStatus(_currentStatus, _currentBpm, _currentMeasuredAt, _currentRetryAttempt);
		}
	}
}