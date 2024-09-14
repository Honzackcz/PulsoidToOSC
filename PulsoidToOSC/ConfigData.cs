using System.IO;
using System.Net;

namespace PulsoidToOSC
{
	internal static class ConfigData
	{
		public static readonly System.Globalization.NumberStyles FloatStyle = System.Globalization.NumberStyles.AllowDecimalPoint | System.Globalization.NumberStyles.AllowLeadingWhite | System.Globalization.NumberStyles.AllowTrailingWhite;
		public static readonly System.Globalization.CultureInfo FloatLocal = System.Globalization.CultureInfo.CreateSpecificCulture("en-US");

		private const string FilePath = "config.txt";
		// General
		private static string _pulsoidToken = string.Empty;
		private static bool _autoStart = true;
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
		private static int _hrFloatMin = 0;
		private static int _hrFloatMax = 255;
		private static float _hrTrendMin = 2f;
		private static float _hrTrendMax = 2f;
		private static int _hrOffset = 0;
		// UI
		private static string _uiColorError = "#FF0000";
		private static string _uiColorWarning = "#FFFF00";
		private static string _uiColorRunning = "#00FF00";
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
		public static int HrFloatMin
		{
			get => _hrFloatMin;
			set => _hrFloatMin = value;
		}
		public static int HrFloatMax
		{
			get => _hrFloatMax;
			set => _hrFloatMax = value;
		}
		public static float HrTrendMin
		{
			get => _hrTrendMin;
			set => _hrTrendMin = value;
		}
		public static float HrTrendMax
		{
			get => _hrTrendMax;
			set => _hrTrendMax = value;
		}
		public static int HrOffset
		{
			get => _hrOffset;
			set => _hrOffset = value;
		}
		// UI
		public static string UIColorError
		{
			get => _uiColorError;
			set => _uiColorError = value;
		}
		public static string UIColorWarning
		{
			get => _uiColorWarning;
			set => _uiColorWarning = value;
		}
		public static string UIColorRunning
		{
			get => _uiColorRunning;
			set => _uiColorRunning = value;
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
			writer.WriteLine($"pulsoidToken={PulsoidToken.ReplaceLineEndings("\\n").Replace("=", string.Empty)}");
			writer.WriteLine($"autoStart={AutoStart}");
			// OSC
			writer.WriteLine($"oscUseManualConfig={OSCUseManualConfig}");
			writer.WriteLine($"oscIP={OSCIP}");
			writer.WriteLine($"oscPort={OSCPort}");
			writer.WriteLine($"oscPath={OSCPath.ReplaceLineEndings("\\n").Replace("=", string.Empty)}");
			// VRChat
			writer.WriteLine($"vrcUseAutoConfig={VRCUseAutoConfig}");
			writer.WriteLine($"vrcSendToAllClinetsOnLAN={VRCSendToAllClinetsOnLAN}");
			writer.WriteLine($"vrcSendBPMToChatbox={VRCSendBPMToChatbox}");
			writer.WriteLine($"vrcChatboxMessage={VRCChatboxMessage.ReplaceLineEndings("\\n").Replace("=", string.Empty)}");
			// Heart rate
			writer.WriteLine($"hrFloatMin={HrFloatMin}");
			writer.WriteLine($"hrFloatMax={HrFloatMax}");
			writer.WriteLine($"hrTrendMin={HrTrendMin.ToString(FloatLocal)}");
			writer.WriteLine($"hrTrendMax={HrTrendMax.ToString(FloatLocal)}");
			writer.WriteLine($"hrOffset={HrOffset}");
			// UI
			writer.WriteLine($"uiColorError={UIColorError}");
			writer.WriteLine($"uiColorWarning={UIColorWarning}");
			writer.WriteLine($"uiColorRunning={UIColorRunning}");
			// Parameters
			foreach (OSCParameter parameter in OSCParameters)
			{
				writer.WriteLine($"oscParameter={parameter.Type};{parameter.Name.ReplaceLineEndings("\\n").Replace("=", string.Empty).Replace(";", string.Empty)}");
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
			string? hrFloatMin = null;
			string? hrFloatMax = null;
			string? hrTrendMin = null;
			string? hrTrendMax = null;
			string? hrOffset = null;
			// UI
			string? uiColorError = null;
			string? uiColorWarning = null;
			string? uiColorRunning = null;
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
							case "hrFloatMin":
								hrFloatMin = value;
								break;
							case "hrFloatMax":
								hrFloatMax = value;
								break;
							case "hrTrendMin":
								hrTrendMin = value;
								break;
							case "hrTrendMax":
								hrTrendMax = value;
								break;
							case "hrOffset":
								hrOffset = value;
								break;
							// UI
							case "uiColorError":
								uiColorError = value;
								break;
							case "uiColorWarning":
								uiColorWarning = value;
								break;
							case "uiColorRunning":
								uiColorRunning = value;
								break;
							// Parameters
							case "oscParameter":
								string[] parameterParts = value.Split(';');
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
			if (vrcChatboxMessage != null) VRCChatboxMessage = vrcChatboxMessage;
			// Heart rate
			if (int.TryParse(hrFloatMin, out int parsedHrFloatMin) && parsedHrFloatMin <= 255 && parsedHrFloatMin >= 0) HrFloatMin = parsedHrFloatMin;
			if (int.TryParse(hrFloatMax, out int parsedHrFloatMax) && parsedHrFloatMax <= 255 && parsedHrFloatMax >= 0) HrFloatMax = parsedHrFloatMax;
			if (float.TryParse(hrTrendMin, FloatStyle, FloatLocal, out float parsedHrTrendMin) && parsedHrTrendMin <= 255f && parsedHrTrendMin >= 0.1) HrTrendMin = parsedHrTrendMin;
			if (float.TryParse(hrTrendMax, FloatStyle, FloatLocal, out float parsedHrTrendMax) && parsedHrTrendMax <= 255f && parsedHrTrendMax >= 0.1) HrTrendMax = parsedHrTrendMax;
			if (int.TryParse(hrOffset, out int parsedHrOffset) && parsedHrOffset < 255 && parsedHrOffset > -255) HrOffset = parsedHrOffset;
			// UI
			if (uiColorError != null && MyRegex.RGBHexCode().IsMatch(uiColorError)) UIColorError = uiColorError;
			if (uiColorWarning != null && MyRegex.RGBHexCode().IsMatch(uiColorWarning)) UIColorWarning = uiColorWarning;
			if (uiColorRunning != null && MyRegex.RGBHexCode().IsMatch(uiColorRunning)) UIColorRunning = uiColorRunning;
			// Parameters
			if (oscParameters.Count > 0) OSCParameters = oscParameters;
		}
	}
}