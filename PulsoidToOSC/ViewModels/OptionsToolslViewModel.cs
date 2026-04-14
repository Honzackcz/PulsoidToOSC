using System.Windows;
using System.Windows.Input;

namespace PulsoidToOSC
{
	internal class OptionsToolslViewModel : ViewModelBase
	{
		private readonly OptionsViewModel _optionsViewModel;

		private readonly Dictionary<MainViewModel.StartButtonType, string> TestHeartRateButtonContents = new()
		{
			{MainViewModel.StartButtonType.Disabled, "Test heart rate"},
			{MainViewModel.StartButtonType.Start, "Test heart rate"},
			{MainViewModel.StartButtonType.Stop, "Stop testing"},
		};

		private MainViewModel.StartButtonType _testHeartRateButton = MainViewModel.StartButtonType.Start;
		private string _minHeartRate = MainProgram.TestHeartRate.MinHeartRate.ToString();
		private string _maxHeartRate = MainProgram.TestHeartRate.MaxHeartRate.ToString();
		private string _incrementStep = MainProgram.TestHeartRate.IncrementStep.ToString();
		private string _incrementInterval = MainProgram.TestHeartRate.IncrementInterval.ToString();

		public MainViewModel.StartButtonType TestHeartRateButton
		{
			get => _testHeartRateButton;
			set
			{
				if (MainProgram.TestHeartRate.Running)
				{
					_testHeartRateButton = value;
				}
				else
				{
					_testHeartRateButton = value == MainViewModel.StartButtonType.Stop ? MainViewModel.StartButtonType.Disabled : value;
				}
				OnPropertyChanged(nameof(TestHeartRateButtonContent));
				OnPropertyChanged(nameof(TestHeartRateButtonEnabled));
			}
		}
		public string TestHeartRateButtonContent
		{
			get => TestHeartRateButtonContents[_testHeartRateButton];
		}
		public bool TestHeartRateButtonEnabled
		{
			get => _testHeartRateButton != MainViewModel.StartButtonType.Disabled;
		}

		public string MinHeartRate
		{
			get => _minHeartRate;
			set
			{
				string newText = value ?? string.Empty;

				if (newText == string.Empty)
				{
					_minHeartRate = newText;
				}
				else if (int.TryParse(newText, out int result))
				{
					result = Math.Clamp(result, 1, 255);
					_minHeartRate = result.ToString();
					MainProgram.TestHeartRate.MinHeartRate = result;
				}

				OnPropertyChanged();
			}
		}
		public string MaxHeartRate
		{
			get => _maxHeartRate;
			set
			{
				string newText = value ?? string.Empty;

				if (newText == string.Empty)
				{
					_maxHeartRate = newText;
				}
				else if (int.TryParse(newText, out int result))
				{
					result = Math.Clamp(result, 1, 255);
					_maxHeartRate = result.ToString();
					MainProgram.TestHeartRate.MaxHeartRate = result;
				}

				OnPropertyChanged();
			}
		}
		public string IncrementStep
		{
			get => _incrementStep;
			set
			{
				string newText = value ?? string.Empty;

				if(newText == string.Empty)
				{
					_incrementStep = newText;
				}
				else if (int.TryParse(newText, out int result))
				{
					result = Math.Clamp(result, 1, 10);
					_incrementStep = result.ToString();
					MainProgram.TestHeartRate.IncrementStep = result;
				}

				OnPropertyChanged();
			}
		}
		public string IncrementInterval
		{
			get => _incrementInterval;
			set
			{
				string newText = value ?? string.Empty;

				if (newText == string.Empty)
				{
					_incrementInterval = newText;
				}
				else if (int.TryParse(newText, out int result))
				{
					result = Math.Clamp(result, 1, 100);
					_incrementInterval = result.ToString();
					MainProgram.TestHeartRate.IncrementInterval = result;
				}

				OnPropertyChanged();
			}
		}


		public ICommand TestHeartRateCommand { get; }
		public ICommand OptionsToolsApplyCommand { get; }


		public OptionsToolslViewModel(OptionsViewModel optionsViewModel)
		{
			_optionsViewModel = optionsViewModel;
			TestHeartRateCommand = new RelayCommand(TestHeartRate);
			OptionsToolsApplyCommand = new RelayCommand(OptionsApply);
		}


		private void TestHeartRate()
		{
			MainProgram.TestHeartRate.Start();
		}

		public void OptionsApply()
		{
			MinHeartRate = MainProgram.TestHeartRate.MinHeartRate.ToString();
			MaxHeartRate = MainProgram.TestHeartRate.MaxHeartRate.ToString();
			IncrementStep = MainProgram.TestHeartRate.IncrementStep.ToString();
			IncrementInterval = MainProgram.TestHeartRate.IncrementInterval.ToString();
		}
	}
}
