using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PulsoidToOSC
{
	internal class LocaleUI : INotifyPropertyChanged
	{
		public string this[string key]
		=> Locale.GetText(key);
		
		public void RefreshLocales()
		{
			OnPropertyChanged("Item[]"); // refresh bindings
		}

		public event PropertyChangedEventHandler? PropertyChanged;
		protected void OnPropertyChanged(string name)
		=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
	}

	internal static class Locale
	{
		public class LocaleDefinition
		{
			public string FileName { get; set; } = string.Empty;
			[JsonPropertyName("localeCode")]
			public string LocaleCode { get; set; } = string.Empty;
			[JsonPropertyName("localeName")]
			public string LocaleName { get; set; } = string.Empty;
		}

		public static Dictionary<string, LocaleDefinition> LocaleDefinitions = [];

		private const string _defaultLocale = "en";
		private static string _currentLocale = string.Empty;
		public static string DefaultLocale { get => _defaultLocale; }
		public static string CurrentLocale {  get => _currentLocale; }

		private static LocaleData? _defaultLocaleData;
		private static LocaleData? _currentLocaleData;

		public static void FetchLocaleFiles()
		{
			Dictionary<string, LocaleDefinition> files = [];
			Assembly assembly = Assembly.GetExecutingAssembly();

			foreach (string resourceName in assembly.GetManifestResourceNames())
			{
				Debug.WriteLine(resourceName);

				if (resourceName.StartsWith("PulsoidToOSC.Locales.") && resourceName.EndsWith(".json"))
				{
					try
					{
						using Stream stream = assembly.GetManifestResourceStream(resourceName) ?? throw new FileNotFoundException("Resource not found");
						using StreamReader reader = new(stream);
						LocaleDefinition localeDefinition = JsonSerializer.Deserialize<LocaleDefinition>(reader.ReadToEnd()) ?? throw new JsonException("Failed to deserialize locale definition");
						localeDefinition.FileName = resourceName;
						files.Add(localeDefinition.LocaleCode, localeDefinition);
					}
					catch
					{
						continue; // skipping unreadable or invalid files
					}
				}
			}

			LocaleDefinitions = files;
		}

		private static LocaleData? LoadLocaleData(string locale)
		{
			locale = locale == string.Empty ? System.Globalization.CultureInfo.CurrentCulture.Name : locale;
			LocaleData? localeData = null;

			while (true)
			{
				if (LocaleDefinitions.TryGetValue(locale, out LocaleDefinition? localeDefinition))
				{
					try
					{
						using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(localeDefinition.FileName) ?? throw new FileNotFoundException("Resource not found");
						using StreamReader reader = new(stream);

						if (localeData == null || localeData.Messages.Count == 0)
						{
							localeData = JsonSerializer.Deserialize<LocaleData>(reader.ReadToEnd()) ?? throw new JsonException("Failed to deserialize locale data");
						}
						else
						{
							LocaleData newLocaleData = JsonSerializer.Deserialize<LocaleData>(reader.ReadToEnd()) ?? throw new JsonException("Failed to deserialize locale data");

							foreach (KeyValuePair<string, string> message in newLocaleData.Messages)
							{
								if (!localeData.Messages.TryAdd(message.Key, message.Value))
								{
									localeData.Messages[message.Key] ??= message.Value;
								}
							}
						}
					}
					catch
					{
						// skipping unreadable or invalid files, will return null when no locale can be loaded
					}
				}

				int i = locale.LastIndexOf('-');
				if (i == -1) break;
				locale = locale[..i];
			}

			return localeData;
		}

		public static void LoadDefaultLocale()
		{
			_defaultLocaleData = LoadLocaleData(_defaultLocale);
		}

		public static void LoadCurrentLocale(string locale = _defaultLocale)
		{
			_currentLocale = locale;

			if (_currentLocale == _defaultLocale && _defaultLocaleData != null)
			{
				_currentLocaleData = _defaultLocaleData;
			}
			else
			{
				_currentLocaleData = LoadLocaleData(locale);
			}
		}

		public static string GetText(string key, object? arguments = null)
		{
			string? value = null;

			if (!_currentLocaleData?.Messages.TryGetValue(key, out value) ?? true) _defaultLocaleData?.Messages.TryGetValue(key, out value);
			
			if (arguments != null)
			{
				if (arguments is IEnumerable<string> argumentList)
				{
					int i = 0;
					foreach (string arg in argumentList)
					{
						value = value?.Replace($"{{{i}}}", arg);
						i++;
					}
				}
				else
				{
					value = value?.Replace($"{{0}}", arguments.ToString());
				}
			}

			return value ?? key;
		}

		private class LocaleData
		{
			[JsonPropertyName("localeCode")]
			public string LocaleCode { get; set; } = string.Empty;
			[JsonPropertyName("localeName")]
			public string LocaleName { get; set; } = string.Empty;

			[JsonPropertyName("authors")]
			public List<string> Authors { get; set; } = [];

			[JsonPropertyName("messages")]
			public Dictionary<string, string> Messages { get; set; } = [];
		}

		public static class Keys
		{
			public static class General
			{
				public const string Start = "General.Start";
				public const string Stop = "General.Stop";
				public const string OK = "General.OK";
				public const string Cancel = "General.Cancel";
				public const string Done = "General.Done";
				public const string Done_Tooltip = "General.Done.Tooltip";
				public const string Apply = "General.Apply";
				public const string Apply_Tooltip = "General.Apply.Tooltip";
				public const string Options = "General.Options";
				public const string Informations = "General.Informations";
				public const string Quit = "General.Quit";
				public const string Show = "General.Show";
			}

			public static class MainWindow
			{
				public static class Status
				{
					public const string AutoStart = "MainWindow.Status.AutoStart";
					public const string InvalidToken = "MainWindow.Status.InvalidToken";
					public const string Connecting = "MainWindow.Status.Connecting";
					public const string Closing = "MainWindow.Status.Closing";
					public const string Waiting = "MainWindow.Status.Waiting";
					public const string ConnectionError = "MainWindow.Status.ConnectionError";
					public const string DataError = "MainWindow.Status.DataError";
					public const string TestStart = "MainWindow.Status.TestStart";
					public const string RetryConnection = "MainWindow.Status.RetryConnection";
					public const string MeasuredAt = "MainWindow.Status.MeasuredAt";
					public const string BPM = "MainWindow.Status.BPM";
				}
			}

			public static class InfoWindow
			{
				public const string Title = "infoWindow.Title";
				public const string AboutText = "InfoWindow.AboutText";
				public const string GitHub = "InfoWindow.GitHub";
				public const string NewVersion = "InfoWindow.NewVersion";
				public const string Version = "InfoWindow.Version";
			}

			public static class ColorPickerWindow
			{
				public const string Title = "ColorPickerWindow.Title";
				public const string Done_Tooltip = "ColorPickerWindow.Done.Tooltip";
			}

			public static class OptionsWindow
			{ 
				public const string Title = "OptionsWindow.Title";

				public static class General
				{
					public const string Title = "OptionsWindow.General.Title";

					public const string GetPulsoidToken_Title = "OptionsWindow.General.GetPulsoidToken.Title";
					public const string GetPulsoidToken_Button = "OptionsWindow.General.GetPulsoidToken.Button";
					public const string GetPulsoidToken_Tooltip = "OptionsWindow.General.GetPulsoidToken.Tooltip";
					public const string GetPulsoidToken_Text = "OptionsWindow.General.GetPulsoidToken.Text";

					public const string PulsoidTokenStatus_Text = "OptionsWindow.General.PulsoidTokenStatus.Text";
					public const string PulsoidTokenStatus_Valid_Text = "OptionsWindow.General.PulsoidTokenStatus.Valid.Text";
					public const string PulsoidTokenStatus_Invalid_Text = "OptionsWindow.General.PulsoidTokenStatus.Invalid.Text";

					public const string SetPulsoidToken = "OptionsWindow.General.SetPulsoidToken";
					public const string SetPulsoidToken_Tooltip = "OptionsWindow.General.SetPulsoidToken.Tooltip";
					public const string SetPulsoidToken_Field_Tooltip = "OptionsWindow.General.SetPulsoidToken.Field.Tooltip";

					public const string AutoStart = "OptionsWindow.General.AutoStart";
					public const string AutoStart_Tooltip = "OptionsWindow.General.AutoStart.Tooltip";
					public const string StartMinimized = "OptionsWindow.General.StartMinimized";
					public const string StartMinimized_Tooltip = "OptionsWindow.General.StartMinimized.Tooltip";
					public const string MinimizeToTray = "OptionsWindow.General.MinimizeToTray";
					public const string MinimizeToTray_Tooltip = "OptionsWindow.General.MinimizeToTray.Tooltip";

					public const string Locale_SameAsSystem = "OptionsWindow.General.Locale.SameAsSystem";
				}

				public static class OSC
				{
					public const string Title = "OptionsWindow.OSC.Title";
					public const string SendToManualEndpoint = "OptionsWindow.OSC.SendToManualEndpoint";
					public const string SendToManualEndpoint_Tooltip = "OptionsWindow.OSC.SendToManualEndpoint.Tooltip";
					public const string ManualEndpointConfig = "OptionsWindow.OSC.ManualEndpointConfig";
					public const string OSCIP = "OptionsWindow.OSC.OSCIP";
					public const string OSCIP_Tooltip = "OptionsWindow.OSC.OSCIP.Tooltip";
					public const string OSCPort = "OptionsWindow.OSC.OSCPort";
					public const string OSCPort_Tooltip = "OptionsWindow.OSC.OSCPort.Tooltip";
					public const string OSCPath = "OptionsWindow.OSC.OSCPath";
					public const string OSCPath_Tooltip = "OptionsWindow.OSC.OSCPath.Tooltip";
				}

				public static class VRChat
				{
					public const string Title = "OptionsWindow.VRChat.Title";
					public const string UseVRCOSCQuery = "OptionsWindow.VRChat.UseVRCOSCQuery";
					public const string UseVRCOSCQuery_Tooltip = "OptionsWindow.VRChat.UseVRCOSCQuery.Tooltip";
					public const string SendToAllVRCClients = "OptionsWindow.VRChat.SendToAllVRCClients";
					public const string SendToAllVRCClients_Tooltip = "OptionsWindow.VRChat.SendToAllVRCClients.Tooltip";
					public const string SendToVRCChatbox = "OptionsWindow.VRChat.SendToVRCChatbox";
					public const string SendToVRCChatbox_Tooltip = "OptionsWindow.VRChat.SendToVRCChatbox.Tooltip";
					public const string VRCChatboxMessage = "OptionsWindow.VRChat.VRCChatboxMessage";
					public const string VRCChatboxMessage_Tooltip = "OptionsWindow.VRChat.VRCChatboxMessage.Tooltip";
				}

				public static class HeartRate
				{
					public const string Title = "OptionsWindow.HeartRate.Title";
					public const string FloatRange = "OptionsWindow.HeartRate.FloatRange";
					public const string FloatRange_Min = "OptionsWindow.HeartRate.FloatRange.Min";
					public const string FloatRange_Max = "OptionsWindow.HeartRate.FloatRange.Max";
					public const string FloatRange_Min_Tooltip = "OptionsWindow.HeartRate.FloatRange.Min.Tooltip";
					public const string FloatRange_Max_Tooltip = "OptionsWindow.HeartRate.FloatRange.Max.Tooltip";
					public const string TrendRange = "OptionsWindow.HeartRate.TrendRange";
					public const string TrendRange_Min = "OptionsWindow.HeartRate.TrendRange.Min";
					public const string TrendRange_Max = "OptionsWindow.HeartRate.TrendRange.Max";
					public const string TrendRange_Min_Tooltip = "OptionsWindow.HeartRate.TrendRange.Min.Tooltip";
					public const string TrendRange_Max_Tooltip = "OptionsWindow.HeartRate.TrendRange.Max.Tooltip";
					public const string Offset = "OptionsWindow.HeartRate.Offset";
					public const string Offset_Tooltip = "OptionsWindow.HeartRate.Offset.Tooltip";
					public const string UndesiredValues = "OptionsWindow.HeartRate.UndesiredValues";
					public const string UndesiredValues_Tooltip = "OptionsWindow.HeartRate.UndesiredValues.Tooltip";
					public const string RandomValue = "OptionsWindow.HeartRate.RandomValue";
					public const string RandomValue_Tooltip = "OptionsWindow.HeartRate.RandomValue.Tooltip";
				}

				public static class Parameters
				{
					public const string Title = "OptionsWindow.Parameters.Title";
					public const string List = "OptionsWindow.Parameters.List";
					public const string Add_Tooltip = "OptionsWindow.Parameters.Add.Tooltip";
					public const string DataType_Tooltip = "OptionsWindow.Parameters.DataType.Tooltip";
					public const string Name_Tooltip = "OptionsWindow.Parameters.Name.Tooltip";
					public const string Delete_Tooltip = "OptionsWindow.Parameters.Delete.Tooltip";
				}

				public static class UI
				{
					public const string Title = "OptionsWindow.UI.Title";
					public const string TextColors = "OptionsWindow.UI.TextColors";
					public const string UseCustomColors = "OptionsWindow.UI.UseCustomColors";
					public const string UseCustomColors_Tooltip = "OptionsWindow.UI.UseCustomColors.Tooltip";
					public const string ErrorColor = "OptionsWindow.UI.ErrorColor";
					public const string ErrorColor_Tooltip = "OptionsWindow.UI.ErrorColor.Tooltip";
					public const string WarningColor = "OptionsWindow.UI.WarningColor";
					public const string WarningColor_Tooltip = "OptionsWindow.UI.WarningColor.Tooltip";
					public const string RunningColor = "OptionsWindow.UI.RunningColor";
					public const string RunningColor_Tooltip = "OptionsWindow.UI.RunningColor.Tooltip";
				}

				public static class Tools
				{
					public const string Title = "OptionsWindow.Tools.Title";
					public const string HeartRateTesting_Title = "OptionsWindow.Tools.HeartRateTesting.Title";
					public const string HeartRateTesting_Text = "OptionsWindow.Tools.HeartRateTesting.Text";
					public const string HeartRateTesting_Test = "OptionsWindow.Tools.HeartRateTesting.Test";
					public const string HeartRateTesting_Stop = "OptionsWindow.Tools.HeartRateTesting.Stop";
					public const string HeartRateTesting_Tooltip = "OptionsWindow.Tools.HeartRateTesting.Tooltip";
					public const string HeartRateTesting_Min = "OptionsWindow.Tools.HeartRateTesting.Min";
					public const string HeartRateTesting_Min_Tooltip = "OptionsWindow.Tools.HeartRateTesting.Min.Tooltip";
					public const string HeartRateTesting_Max = "OptionsWindow.Tools.HeartRateTesting.Max";
					public const string HeartRateTesting_Max_Tooltip = "OptionsWindow.Tools.HeartRateTesting.Max.Tooltip";
					public const string HeartRateTesting_Step = "OptionsWindow.Tools.HeartRateTesting.Step";
					public const string HeartRateTesting_Step_Tooltip = "OptionsWindow.Tools.HeartRateTesting.Step.Tooltip";
					public const string HeartRateTesting_Interval = "OptionsWindow.Tools.HeartRateTesting.Interval";
					public const string HeartRateTesting_Interval_Tooltip = "OptionsWindow.Tools.HeartRateTesting.Interval.Tooltip";
				}
			}
		}
	}
}
