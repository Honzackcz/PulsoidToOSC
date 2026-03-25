using System.Collections.ObjectModel;
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
				{OSCParameter.Types.Integer, "HR Integer" },
				{OSCParameter.Types.Float, "HR Float [-1, 1]" },
				{OSCParameter.Types.Float01, "HR Float [0, 1]" },
				{OSCParameter.Types.BoolToggle, "Bool Toggle" },
				{OSCParameter.Types.BoolActive, "Bool Active" },
				{OSCParameter.Types.Trend, "Trend [-1, 1]" },
				{OSCParameter.Types.Trend01, "Trend [0, 1]" }
			};
			private static readonly Dictionary<OSCParameter.Types, string> ParameterTypeToolTips = new()
			{
				{OSCParameter.Types.Integer, "Heart rate - Integer [0, 255]" },
				{OSCParameter.Types.Float, "Heart rate - Float ([0, 255] -> [-1, 1])" },
				{OSCParameter.Types.Float01, "Heart rate - Float ([0, 255] -> [0, 1])" },
				{OSCParameter.Types.BoolToggle, "Toggles with each OSC update" },
				{OSCParameter.Types.BoolActive, "True when app is working" },
				{OSCParameter.Types.Trend, "Trend of heart rate change - Float [-1, 1]" },
				{OSCParameter.Types.Trend01, "Trend of heart rate change - Float [0, 1]" }
			};

			private OSCParameter.Types _type = OSCParameter.Types.Integer;
			private string _name = string.Empty;

			public OSCParameter.Types Type
			{
				get => _type;
				set { _type = value; OnPropertyChanged(nameof(TypeName)); }
			}
			public string TypeName
			{
				get => ParameterTypeNames[_type];
			}

			public class ParameterType_Item
			{
				private OSCParameter.Types _itemType;
				public OSCParameter.Types ParameterType_ItemType
				{
					get => _itemType;
					set { _itemType = value; }
				}
				public string ParameterType_ItemName
				{ 
					get => ParameterTypeNames[_itemType];
				}
				public string ParameterType_ItemToolTip
				{ 
					get => ParameterTypeToolTips[_itemType];
				}
			}
			public ObservableCollection<ParameterType_Item> ParameterType_Items { get; set; } =
			[
				new ParameterType_Item() { ParameterType_ItemType = (OSCParameter.Types)0 },
				new ParameterType_Item() { ParameterType_ItemType = (OSCParameter.Types)1 },
				new ParameterType_Item() { ParameterType_ItemType = (OSCParameter.Types)2 },
				new ParameterType_Item() { ParameterType_ItemType = (OSCParameter.Types)3 },
				new ParameterType_Item() { ParameterType_ItemType = (OSCParameter.Types)4 },
				new ParameterType_Item() { ParameterType_ItemType = (OSCParameter.Types)5 },
				new ParameterType_Item() { ParameterType_ItemType = (OSCParameter.Types)6 }
			];
			public ParameterType_Item ParameterType_SelectedItem
			{ 
				get => ParameterType_Items[((int)_type)];
				set
				{
					_type = value.ParameterType_ItemType;
					OnPropertyChanged();
				}
			}

			public string Name
			{
				get => _name;
				set { _name = value?.Replace("=", string.Empty).Replace(";", string.Empty) ?? string.Empty; OnPropertyChanged(); }
			}

			public ICommand DeleteParameterCommand { get; }
			public ICommand ApplyParameterCommand { get; }

			public ParameterItem()
			{
				DeleteParameterCommand = new RelayCommand(DeleteParameter);
				ApplyParameterCommand = new RelayCommand(ApplyParameter);
			}

			private void DeleteParameter()
			{
				ParametersOptionsViewModel?.DeleteParameter(this);
			}

			private void ApplyParameter()
			{
				ParametersOptionsViewModel?._optionsViewModel.OptionsApply();
			}
		}

		public ICommand AddNewParameterCommand { get; }
		public ICommand OptionsParametersApplyCommand { get; }


		public OptionsParametersViewModel(OptionsViewModel optionsViewModel)
		{
			_optionsViewModel = optionsViewModel;
			AddNewParameterCommand = new RelayCommand(AddNewParameter);
			OptionsParametersApplyCommand = new RelayCommand(_optionsViewModel.OptionsApply);
		}

		private void DeleteParameter(ParameterItem parameterItem)
		{
			Parameters.Remove(parameterItem);
		}

		private void AddNewParameter()
		{
			Parameters.Add(new() { ParametersOptionsViewModel = this });
		}

		public void OptionsApply()
		{
			ApplyParameters(false);
		}

		private void ApplyParameters(bool canSaveConfig)
		{
			List<OSCParameter> parameters = [];

			foreach (ParameterItem parameterItem in Parameters)
			{
				parameters.Add(new() { Name = parameterItem.Name, Type = parameterItem.Type });
			}

			ConfigData.OSCParameters = parameters;

			if (canSaveConfig) ConfigData.SaveConfig();
		}
	}
}