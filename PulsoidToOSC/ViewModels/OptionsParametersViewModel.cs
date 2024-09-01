﻿using System.Collections.ObjectModel;
using System.Windows.Input;

namespace PulsoidToOSC
{
	internal class OptionsParametersViewModel : ViewModelBase
	{
		private readonly OptionsViewModel _optionsViewModel;

		public ObservableCollection<ParameterItem> Parameters { get; set; } = [];

		public class ParameterItem : ViewModelBase
		{
			public OptionsParametersViewModel? ParametersOptionsViewModel { get; set; }
			private static readonly Dictionary<OSCParameter.Types, string> ParameterTypeNames = new()
			{
				{OSCParameter.Types.Integer, "Integer" },
				{OSCParameter.Types.Float, "Float [-1, 1]" },
				{OSCParameter.Types.Float01, "Float [0, 1]" },
				{OSCParameter.Types.BoolToggle, "Bool Toggle" },
				{OSCParameter.Types.BoolActive, "Bool Active" },
				{OSCParameter.Types.TrendF, "Trend [-1, 1]" },
				{OSCParameter.Types.TrendF01, "Trend [0, 1]" }
			};

			private OSCParameter.Types _type = OSCParameter.Types.Integer;
			private string _name = string.Empty;

			public OSCParameter.Types Type
			{
				get => _type;
				set { _type = value; OnPropertyChanged(nameof(TypeName)); }
			}

			public string Name
			{
				get => _name;
				set { _name = value?.Replace("=", string.Empty).Replace(";", string.Empty) ?? string.Empty; OnPropertyChanged(); }
			}
			public string TypeName { get => ParameterTypeNames[_type]; }

			public ICommand SetItegerTypeCommand { get; }
			public ICommand SetFloatTypeCommand { get; }
			public ICommand SetFloat01TypeCommand { get; }
			public ICommand SetBoolToggleTypeCommand { get; }
			public ICommand SetBoolActiveTypeCommand { get; }
			public ICommand SetTrendFTypeCommand { get; }
			public ICommand SetTrendF01TypeCommand { get; }
			public ICommand DeleteParameterCommand { get; }

			public ParameterItem()
			{
				SetItegerTypeCommand = new RelayCommand(SetItegerType);
				SetFloatTypeCommand = new RelayCommand(SetFloatType);
				SetFloat01TypeCommand = new RelayCommand(SetFloat01Type);
				SetBoolToggleTypeCommand = new RelayCommand(SetBoolToggleType);
				SetBoolActiveTypeCommand = new RelayCommand(SetBoolActiveType);
				SetTrendFTypeCommand = new RelayCommand(SetTrendFType);
				SetTrendF01TypeCommand = new RelayCommand(SetTrendF01Type);
				DeleteParameterCommand = new RelayCommand(DeleteParameter);
			}

			private void SetItegerType()
			{
				Type = OSCParameter.Types.Integer;
			}

			private void SetFloatType()
			{
				Type = OSCParameter.Types.Float;
			}

			private void SetFloat01Type()
			{
				Type = OSCParameter.Types.Float01;
			}

			private void SetBoolToggleType()
			{
				Type = OSCParameter.Types.BoolToggle;
			}

			private void SetBoolActiveType()
			{
				Type = OSCParameter.Types.BoolActive;
			}

			private void SetTrendFType()
			{
				Type = OSCParameter.Types.TrendF;
			}

			private void SetTrendF01Type()
			{
				Type = OSCParameter.Types.TrendF01;
			}

			private void DeleteParameter()
			{
				ParametersOptionsViewModel?.DeleteParameter(this);
			}
		}

		public ICommand AddNewParameterCommand { get; }
		public ICommand ApplyParametersCommand { get; }

		public OptionsParametersViewModel(OptionsViewModel optionsViewModel)
		{
			_optionsViewModel = optionsViewModel;
			AddNewParameterCommand = new RelayCommand(AddNewParameter);
			ApplyParametersCommand = new RelayCommand(ApplyParameters);
		}

		private void DeleteParameter(ParameterItem parameterItem)
		{
			Parameters.Remove(parameterItem);
		}

		private void AddNewParameter()
		{
			Parameters.Add(new() { ParametersOptionsViewModel = this });
		}

		private void ApplyParameters()
		{
			List<OSCParameter> parameters = [];

			foreach (ParameterItem parameterItem in Parameters)
			{
				parameters.Add(new() { Name = parameterItem.Name, Type = parameterItem.Type });
			}

			ConfigData.OSCParameters = parameters;
			ConfigData.SaveConfig();
		}
	}
}
