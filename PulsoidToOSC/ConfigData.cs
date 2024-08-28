using System.IO;
using System.Net;

namespace PulsoidToOSC
{
	internal static class ConfigData
	{
		private const string FilePath = "config.txt";
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
		private static string _vrcChatboxMessage = "Heart rate:\\v<bpm> BPM <trend>";
		// Heart rate
		private static float _hrTrendMax = 2f;
		private static float _hrTrendMin = 2f;
		// Parameters
		private static List<OSCParameter> _oscParameters =
		[
			new() { Type = OSCParameter.Types.Integer, Name = "HeartRateInt" },
			new() { Type = OSCParameter.Types.Integer, Name = "HeartRate3" },
			new() { Type = OSCParameter.Types.Float, Name = "HeartRateFloat" },
			new() { Type = OSCParameter.Types.Float, Name = "HeartRate" },
			new() { Type = OSCParameter.Types.Float01, Name = "HeartRateFloat01" },
			new() { Type = OSCParameter.Types.Float01, Name = "HeartRate2" },
			new() { Type = OSCParameter.Types.BoolToggle, Name = "HeartBeatToggle" },
		];

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
		// Heart rate
		public static float HrTrendMax
		{
			get => _hrTrendMax;
			set => _hrTrendMax = value;
		}
		public static float HrTrendMin
		{
			get => _hrTrendMin;
			set => _hrTrendMin = value;
		}
		// Parameters
		public static List<OSCParameter> OSCParameters
		{
			get => _oscParameters;
			set => _oscParameters = value;
		}

		public static void SaveConfig()
		{
			using StreamWriter writer = new(FilePath);
			// General
			writer.WriteLine($"pulsoidToken={PulsoidToken.ReplaceLineEndings("\\n").Replace("=", "")}");
			writer.WriteLine($"autoStart={AutoStart}");
			// OSC
			writer.WriteLine($"oscUseManualConfig={OSCUseManualConfig}");
			writer.WriteLine($"oscIP={OSCIP}");
			writer.WriteLine($"oscPort={OSCPort}");
			writer.WriteLine($"oscPath={OSCPath.ReplaceLineEndings("\\n").Replace("=", "")}");
			// VRChat
			writer.WriteLine($"vrcUseAutoConfig={VRCUseAutoConfig}");
			writer.WriteLine($"vrcSendToAllClinetsOnLAN={VRCSendToAllClinetsOnLAN}");
			writer.WriteLine($"vrcSendBPMToChatbox={VRCSendBPMToChatbox}");
			writer.WriteLine($"vrcChatboxMessage={VRCChatboxMessage.ReplaceLineEndings("\\n").Replace("=", "")}");
			// Heart rate
			writer.WriteLine($"hrTrendMax={HrTrendMax}");
			writer.WriteLine($"hrTrendMin={HrTrendMin}");
			// Parameters
			foreach (OSCParameter parameter in OSCParameters)
			{
				writer.WriteLine($"oscParameter={parameter.Type};{parameter.Name.ReplaceLineEndings("\\n").Replace("=", "").Replace(";", "")}");
			}
		}

		public static void LoadConfig()
		{
			if (!File.Exists(FilePath)) return;
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
			// Heart rate
			string? hrTrendMax = null;
			string? hrTrendMin = null;
			// Parameters
			List<OSCParameter> oscParameters = [];

			using (StreamReader reader = new(FilePath))
			{
				string? line;
				while ((line = reader.ReadLine()) != null)
				{
					string[] parts = line.Split('=');

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
							// Heart rate
							case "hrTrendMax":
								hrTrendMax = value;
								break;
							case "hrTrendMin":
								hrTrendMin = value;
								break;
							// Parameters
							case "oscParameter":
								string[] parameterParts = value.Split(";");
								if (parameterParts.Length == 2 && parameterParts[1] != string.Empty && Enum.TryParse(parameterParts[0], true, out OSCParameter.Types type))
								{
									string name = parameterParts[1];
									oscParameters.Add(new() { Name = name, Type = type });
								}
								break;

							default:
								break;
						}
					}
				}
			}
			// General
			if (pulsoidToken != null && MyRegex.GUID().IsMatch(pulsoidToken)) PulsoidToken = pulsoidToken;
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
			// Heart rate
			if (float.TryParse(hrTrendMax, out float parsedHrTrendMax) && parsedHrTrendMax <= 65535 && parsedHrTrendMax > 0) HrTrendMax = parsedHrTrendMax;
			if (float.TryParse(hrTrendMin, out float parsedHrTrendMin) && parsedHrTrendMin <= 65535 && parsedHrTrendMin > 0) HrTrendMin = parsedHrTrendMin;
			// Parameters
			if (oscParameters.Count > 0) OSCParameters = oscParameters;
		}
	}
}