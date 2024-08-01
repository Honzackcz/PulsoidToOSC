using System.Windows.Input;

namespace PulsoidToOSC
{
	public class MainViewModel : ViewModelBase
	{
		public OptionsViewModel OptionsViewModel { get; }
		public MainWindow? MainWindow { get; set; }

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

		public MainViewModel()
		{
			OptionsViewModel = new OptionsViewModel(this);

			StartCommand = new RelayCommand(Start);
			OpenOptionsCommand = new RelayCommand(OpenOptions);
		}

		private void Start()
		{
			MainProgram.StartPulsoidToOSC();
		}

		private void OpenOptions()
		{
			OptionsViewModel.OpenOptions();
		}
	}
}