using System.Windows.Input;

namespace PulsoidToOSC
{
	internal class MainViewModel : ViewModelBase
	{
		private const string ColorGreen = "#00FF00";
		private const string ColorRed = "#FF0000";
		private const string ColorYellow = "#FFFF00";
		private const string ColorCyan = "#00FFFF";
		public enum Colors {None, Green, Red, Yellow, Cyan };

		public OptionsViewModel OptionsViewModel { get; }
		public InfoViewModel InfoViewModel { get; }
		public MainWindow? MainWindow { get; private set; }

		private string _bpmText = string.Empty;
		private string _measuredAtText = string.Empty;
		private string _startButtonContent = "Start";
		private string _errorText = string.Empty;
		private string _errorTextColor = "#000000";
		private string _liveIndicatorColor = "#00000000";

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

		public void SetUI(string errorText = "", Colors indicatorColor = Colors.None, string bpmText = "", string measuredAtText = "")
		{
			string hexColor = indicatorColor switch
			{
				Colors.Green => ColorGreen,
				Colors.Red => ColorRed,
				Colors.Yellow => ColorYellow,
				Colors.Cyan => ColorCyan,
				_ => "#00000000"
			};

			ErrorText = errorText;
			ErrorTextColor = hexColor;
			LiveIndicatorColor = hexColor;
			BPMText = bpmText;
			MeasuredAtText = measuredAtText;
		}
	}
}