﻿using System.IO;
using System.Net;

namespace PulsoidToOSC
{
	internal static class ConfigData
	{
		private const string filePath = "config.txt";
		// General
		private static string _pulsoidToken = "";
		private static bool _autoStart = false;
		// OSC
		private static bool _oscUseManualConfig = true;
		private static IPAddress _oscIP = IPAddress.Parse("127.0.0.1");
		private static int _oscPort = 9000;
		private static string _oscPath = "/avatar/parameters/";
		// VRChat
		private static bool _vrcUseAutoConfig = false;
		private static bool _vrcSendToAllClinetsOnLAN = false;
		private static bool _vrcSendBPMToChatbox = false;
		private static string _vrcChatboxMessage = "Heart rate: <bpm> BPM";

		// General
		public static string PulsoidToken
		{
			get => _pulsoidToken;
			set => _pulsoidToken = value;
		}
		public static bool AutoStart
		{
			get => _autoStart;
			set => _autoStart = value;
		}
		// OSC
		public static bool OSCUseManualConfig
		{
			get => _oscUseManualConfig;
			set => _oscUseManualConfig = value;
		}
		public static IPAddress OSCIP
		{
			get => _oscIP;
			set => _oscIP = value;
		}
		public static int OSCPort
		{
			get => _oscPort;
			set => _oscPort = value;
		}
		public static string OSCPath
		{
			get => _oscPath;
			set => _oscPath = value;
		}
		// VRChat
		public static bool VRCUseAutoConfig
		{
			get => _vrcUseAutoConfig;
			set => _vrcUseAutoConfig = value;
		}
		public static bool VRCSendToAllClinetsOnLAN
		{
			get => _vrcSendToAllClinetsOnLAN;
			set => _vrcSendToAllClinetsOnLAN = value;
		}
		public static bool VRCSendBPMToChatbox
		{
			get => _vrcSendBPMToChatbox;
			set => _vrcSendBPMToChatbox = value;
		}
		public static string VRCChatboxMessage
		{
			get => _vrcChatboxMessage;
			set => _vrcChatboxMessage = value;
		}

		public static void SaveConfig()
		{
			using StreamWriter writer = new(filePath);
			// General
			writer.WriteLine($"pulsoidToken={PulsoidToken.ReplaceLineEndings("\\n")}");
			writer.WriteLine($"autoStart={AutoStart}");
			// OSC
			writer.WriteLine($"oscUseManualConfig={OSCUseManualConfig}");
			writer.WriteLine($"oscIP={OSCIP}");
			writer.WriteLine($"oscPort={OSCPort}");
			writer.WriteLine($"oscPath={OSCPath.ReplaceLineEndings("\\n")}");
			// VRChat
			writer.WriteLine($"vrcUseAutoConfig={VRCUseAutoConfig}");
			writer.WriteLine($"vrcSendToAllClinetsOnLAN={VRCSendToAllClinetsOnLAN}");
			writer.WriteLine($"vrcSendBPMToChatbox={VRCSendBPMToChatbox}");
			writer.WriteLine($"vrcChatboxMessage={VRCChatboxMessage.ReplaceLineEndings("\\n")}");
		}

		public static void LoadConfig()
		{
			if (!File.Exists(filePath)) return;
			// General
			string? pulsoidToken = null;
			string? autoStart = null;
			// OSC
			string? oscUseManualConfig = null;
			string? oscIP = null;
			string? oscPort = null;
			string? oscPath = null;
			// VRChat
			string? vrcUseAutoConfig = null;
			string? vrcSendToAllClinetsOnLAN = null;
			string? vrcSendBPMToChatbox = null;
			string? vrcChatboxMessage = null;

			using (StreamReader reader = new(filePath))
			{
				string? line;
				while ((line = reader.ReadLine()) != null)
				{
					var parts = line.Split('=');

					if (parts.Length == 2)
					{
						string key = parts[0].Trim();
						string value = parts[1].Trim();

						switch (key)
						{
							// General
							case "pulsoidToken":
								pulsoidToken = value;
								break;
							case "autoStart":
								autoStart = value;
								break;
							// OSC
							case "oscUseManualConfig":
								oscUseManualConfig = value;
								break;
							case "oscIP":
								oscIP = value;
								break;
							case "oscPort":
								oscPort = value;
								break;
							case "oscPath":
								oscPath = value;
								break;
							// VRChat
							case "vrcUseAutoConfig":
								vrcUseAutoConfig = value;
								break;
							case "vrcSendToAllClinetsOnLAN":
								vrcSendToAllClinetsOnLAN = value;
								break;
							case "vrcSendBPMToChatbox":
								vrcSendBPMToChatbox = value;
								break;
							case "vrcChatboxMessage":
								vrcChatboxMessage = value;
								break;

							default:
								break;
						}
					}
				}
			}
			// General
			if (pulsoidToken != null && MyRegex.RegexGUID().IsMatch(pulsoidToken)) PulsoidToken = pulsoidToken;
			if (bool.TryParse(autoStart, out bool parsedAutoStart)) AutoStart = parsedAutoStart;
			// OSC
			if (bool.TryParse(oscUseManualConfig, out bool parsedOSCUseManualConfig)) OSCUseManualConfig = parsedOSCUseManualConfig;
			if (IPAddress.TryParse(oscIP, out IPAddress? parsedOSCIP)) OSCIP = parsedOSCIP;
			if (Int32.TryParse(oscPort, out int parsedOSCPort) && parsedOSCPort <= 65535 && parsedOSCPort > 0) OSCPort = parsedOSCPort;
			if (oscPath != null)
			{
				if (!oscPath.StartsWith('/')) oscPath = "/" + oscPath;
				if (!oscPath.EndsWith('/')) oscPath += "/";
				OSCPath = oscPath;
			}
			// VRChat
			if (bool.TryParse(vrcUseAutoConfig, out bool parsedVRCUseAutoConfig)) VRCUseAutoConfig = parsedVRCUseAutoConfig;
			if (bool.TryParse(vrcSendToAllClinetsOnLAN, out bool parsedVRCSendToAllClinetsOnLAN)) VRCSendToAllClinetsOnLAN = parsedVRCSendToAllClinetsOnLAN;
			if (bool.TryParse(vrcSendBPMToChatbox, out bool parsedVRCSendBPMToChatbox)) VRCSendBPMToChatbox = parsedVRCSendBPMToChatbox;
			if (vrcChatboxMessage != null)
			{
				VRCChatboxMessage = vrcChatboxMessage;
			}
		}
	}
}