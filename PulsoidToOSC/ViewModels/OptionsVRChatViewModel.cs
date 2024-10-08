﻿using System.Windows.Input;

namespace PulsoidToOSC
{
	internal class OptionsVRChatViewModel : ViewModelBase
	{
		private readonly OptionsViewModel _optionsViewModel;

		private bool _vrcAutoConfigCheckmark = false;
		private bool _vrcClinetsOnLANCheckmark = false;
		private bool _vrcChatboxCheckmark = false;
		private string _vrcChatboxMessageText = string.Empty;

		public bool VRCAutoConfigCheckmark
		{
			get => _vrcAutoConfigCheckmark;
			set { _vrcAutoConfigCheckmark = value; OnPropertyChanged(); ToggleVRCAutoConfig(); }
		}
		public bool VRCClinetsOnLANCheckmark
		{
			get => _vrcClinetsOnLANCheckmark;
			set { _vrcClinetsOnLANCheckmark = value; OnPropertyChanged(); ToggleVRCClinetsOnLAN(); }
		}
		public bool VRCChatboxCheckmark
		{
			get => _vrcChatboxCheckmark;
			set { _vrcChatboxCheckmark = value; OnPropertyChanged(); ToggleVRCChatbox(); }
		}
		public string VRCChatboxMessageText
		{
			get => _vrcChatboxMessageText;
			set { _vrcChatboxMessageText = value?.Replace("=", string.Empty) ?? string.Empty; OnPropertyChanged(); }
		}

		public ICommand OptionsVRChatApplyCommand { get; }

		public OptionsVRChatViewModel(OptionsViewModel optionsViewModel)
		{
			_optionsViewModel = optionsViewModel;
			OptionsVRChatApplyCommand = new RelayCommand(_optionsViewModel.OptionsApply);
		}

		private void ToggleVRCAutoConfig()
		{
			if (ConfigData.VRCUseAutoConfig == VRCAutoConfigCheckmark) return;
			ConfigData.VRCUseAutoConfig = VRCAutoConfigCheckmark;
			ConfigData.SaveConfig();
		}

		private void ToggleVRCClinetsOnLAN()
		{
			if (ConfigData.VRCSendToAllClinetsOnLAN == VRCClinetsOnLANCheckmark) return;
			ConfigData.VRCSendToAllClinetsOnLAN = VRCClinetsOnLANCheckmark;
			ConfigData.SaveConfig();
		}

		private void ToggleVRCChatbox()
		{
			if (ConfigData.VRCSendBPMToChatbox == VRCChatboxCheckmark) return;
			ConfigData.VRCSendBPMToChatbox = VRCChatboxCheckmark;
			ConfigData.SaveConfig();
		}

		public void OptionsApply()
		{
			SetVRCChatboxMessage(false);
		}

		private void SetVRCChatboxMessage(bool canSaveConfig)
		{
			if (ConfigData.VRCChatboxMessage == VRCChatboxMessageText) return;
			ConfigData.VRCChatboxMessage = VRCChatboxMessageText;
			if (canSaveConfig) ConfigData.SaveConfig();
		}
	}
}
