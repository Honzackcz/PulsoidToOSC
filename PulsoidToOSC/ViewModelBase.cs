using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace PulsoidToOSC
{
	internal class ViewModelBase : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler? PropertyChanged;

		protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}

	internal class RelayCommand(Action execute, Func<bool>? canExecute = null) : ICommand
	{
		private readonly Action _execute = execute ?? throw new ArgumentNullException(nameof(execute));
		private readonly Func<bool>? _canExecute = canExecute;

		public bool CanExecute(object? parameter) => _canExecute == null || _canExecute();
		public void Execute(object? parameter) => _execute();
		public event EventHandler? CanExecuteChanged
		{
			add { CommandManager.RequerySuggested += value; }
			remove { CommandManager.RequerySuggested -= value; }
		}
	}
}