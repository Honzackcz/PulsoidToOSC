using SharpOSC;
using System.Text.Json;
using System.Windows.Threading;
using System.Net.WebSockets;

namespace PulsoidToOSC
{
	public static class MainProgram
	{
		public const string AppVersion = "v0.1.0b";
		public const string GitHubOwner = "Honzackcz";
		public const string GitHubRepo = "PulsoidToOSC";

		private const string ColorGreen = "#00FF00";
		private const string ColorRed = "#FF0000";
		private const string ColorYellow = "#FFFF00";
		private const string ColorCyan = "#00FFFF";

		private enum AppSates { Stopped, Starting, Running, Stopping };
		private static AppSates appSate;
		public static readonly Dispatcher Disp = Dispatcher.CurrentDispatcher;
		public static readonly MainViewModel MainViewModel = new();

		public enum HeartRateTrends { None, Stable, Upward, Downward, StrongUpward, StrongDownward };
		public static HeartRateTrends HeartRateTrend { get; private set; } = HeartRateTrends.None;
		private static readonly List<int> recentHeartRates = [];

		public static UDPSender? OSCSender { get; private set; }
		public static bool HBToggle { get; private set; } = false;
		private static DateTime lastWSMessageTime = DateTime.MinValue;
		private static bool wsMessageTimeout = false;
		private static int failedWSConnectionAttempts = 0;
		private static CancellationTokenSource delayedStartTaskCts = new();

		public static void StartUp()
		{
			ConfigData.LoadConfig();

			if (!MyRegex.GUID().IsMatch(ConfigData.PulsoidToken)) PulsoidApi.TokenValiditi = PulsoidApi.TokenValidities.Invalid;

			MainWindow mainWindow = new()
			{
				DataContext = MainViewModel
			};
			MainViewModel.MainWindow = mainWindow;
			mainWindow.Show();

			SetupOSC();
			SetupWebSocketEvents();

			if (ConfigData.AutoStart)
			{
				SetUI("Auto start...", ColorYellow);

				_ = Task.Run(async () =>
				{
					await Task.Delay(500, delayedStartTaskCts.Token);
					if (appSate == AppSates.Stopped) Disp.Invoke(() => StartPulsoidToOSC());
				}, delayedStartTaskCts.Token);
			}

			_ = Task.Run(async () => 
			{
				MainViewModel.InfoViewModel.IsNewVersionAvailable = await GitHubApi.IsNewVersionAvailable(GitHubOwner, GitHubRepo, AppVersion) == GitHubApi.VersionStatus.NewIsAvailable;
			});
		}

		public static void StartPulsoidToOSC(bool reconnect = false)
		{
			if (!reconnect) failedWSConnectionAttempts = 0;
			delayedStartTaskCts.Cancel();

			if (appSate == AppSates.Running)
			{
				_ = StopPulsoidToOSC();
				return;
			}

			if (appSate != AppSates.Stopped) return;

			if (PulsoidApi.TokenValiditi == PulsoidApi.TokenValidities.Invalid)
			{
				SetUI("Invalid Pulsoid token!\nIn options setup valid token.", ColorRed);
				return;
			}

			appSate = AppSates.Starting;
			MainViewModel.StartButtonContent = " ";
			SetUI("Connecting to Pulsoid...", ColorYellow);
			HeartRateTrend = HeartRateTrends.None;
			recentHeartRates.Clear();
			SetupOSC();
			VRCOSC.Query.SetupQuerry();
			_ = StartWebSocket();
		}

		public static async Task StopPulsoidToOSC()
		{
			if (appSate == AppSates.Stopped || appSate == AppSates.Stopping) return;

			appSate = AppSates.Stopping;
			MainViewModel.StartButtonContent = " ";
			SetUI("Closing connection to Pulsoid...", ColorYellow);
			VRCOSC.Query.StopQuerry();

			if (SimpleWSClient.ClientState == WebSocketState.Open)
			{
				await SimpleWSClient.CloseConnectionAsync();
			}

			SendHeartRates();

			appSate = AppSates.Stopped;
			MainViewModel.StartButtonContent = "Start";
			SetUI();
		}

		public static async void RestartPulsoidToOSC()
		{
			if (appSate == AppSates.Stopped || appSate == AppSates.Stopping) return;
			await StopPulsoidToOSC();
			StartPulsoidToOSC();
		}

		private static void SetupOSC()
		{
			OSCSender?.Close();
			OSCSender = new UDPSender(ConfigData.OSCIP.ToString(), ConfigData.OSCPort);
		}

		private static void SetupWebSocketEvents()
		{
			SimpleWSClient.OnOpen += () => Disp.Invoke(() => OnWSOpen());
			SimpleWSClient.OnMessage += (message) => Disp.Invoke(() => OnWSMessage(message));
			SimpleWSClient.OnClose += (response) => Disp.Invoke(() => OnWSClose(response));
		}

		private static async Task StartWebSocket()
		{
			await SimpleWSClient.OpenConnectionAsync(PulsoidApi.PulsoidWSURL + ConfigData.PulsoidToken);

			lastWSMessageTime = DateTime.MinValue;

			await Task.Delay(1000);
			while (SimpleWSClient.ClientState == WebSocketState.Open)
			{
				if (!wsMessageTimeout && lastWSMessageTime != DateTime.MinValue && lastWSMessageTime.AddSeconds(10) < DateTime.UtcNow)
				{
					wsMessageTimeout = true;
					SendHeartRates();

					SetUI("Waiting for heart rate...", ColorYellow);
				}
				await Task.Delay(100);
			}
		}

		private static void OnWSOpen()
		{
			appSate = AppSates.Running;
			MainViewModel.StartButtonContent = "Stop";

			PulsoidApi.TokenValiditi = PulsoidApi.TokenValidities.Valid;
			failedWSConnectionAttempts = 0;

			SetUI("Waiting for heart rate...", ColorYellow);
		}

		private static void OnWSClose(SimpleWSClient.Response response)
		{
			_ = StopPulsoidToOSC();

			if (response.HttpStatusCode == 400 || response.HttpStatusCode == 401 || response.HttpStatusCode == 403) //Invalid token
			{
				PulsoidApi.TokenValiditi = PulsoidApi.TokenValidities.Invalid;

				SetUI("Invalid Pulsoid token!\nIn options setup valid token.", ColorRed);
			}
			else if (response.WebSocketCloseStatusCode > 1000 && (appSate == AppSates.Stopping || appSate == AppSates.Stopped) && SimpleWSClient.ClientState != WebSocketState.Open && SimpleWSClient.ClientState != WebSocketState.Connecting) //Connection lost
			{
				SetUI("Error: Connection to Pulsoid!", ColorRed);

				if (failedWSConnectionAttempts < 5 && PulsoidApi.TokenValiditi != PulsoidApi.TokenValidities.Invalid)
				{
					SetUI($"Error: Connection to Pulsoid!\nRetrying connection... ({failedWSConnectionAttempts + 1})", ColorRed);

					delayedStartTaskCts = new();
					Task.Run(async () =>
					{
						await Task.Delay(failedWSConnectionAttempts * 10000, delayedStartTaskCts.Token);
						if (appSate == AppSates.Stopped) Disp.Invoke(() => StartPulsoidToOSC(true));
					}, delayedStartTaskCts.Token);

					failedWSConnectionAttempts++;
				}
			}
		}

		private static void OnWSMessage(string message)
		{
			lastWSMessageTime = DateTime.UtcNow;
			wsMessageTimeout = false;

			long measuredAt = 0L;
			int heartRate = 0;

			PulsoidApi.Json.WSMessage? messageJson = JsonSerializer.Deserialize<PulsoidApi.Json.WSMessage>(message);

			if (messageJson != null)
			{
				measuredAt = messageJson.MeasuredAt ?? 0L;

				if (messageJson.Data != null)
				{
					heartRate = messageJson.Data.HeartRate ?? 0;
				}
			}

			SendHeartRates(heartRate);

			if (heartRate > 0) SetUI("", HBToggle ? ColorCyan : ColorGreen, $"BPM: {heartRate}", measuredAt > 0 ? "Measured at: " + DateTimeOffset.FromUnixTimeMilliseconds(measuredAt).LocalDateTime.ToLongTimeString() : "");
			else SetUI("Error at obtaing heart rate data!", ColorRed);
		}

		private static void SendHeartRates(int heartRate = 0)
		{
			if (heartRate < 0) return;

			HBToggle = !HBToggle;

			recentHeartRates.Add(heartRate);
			if (recentHeartRates.Count > 5)
			{
				recentHeartRates.RemoveAt(0);
				HeartRateTrend = CalcualteTrend(recentHeartRates) switch
				{
					> 1f => HeartRateTrends.StrongUpward,
					< -1f => HeartRateTrends.StrongDownward,
					> 0.5f => HeartRateTrends.Upward,
					< -0.5f => HeartRateTrends.Downward,
					_ => HeartRateTrends.Stable
				};
			}
			else
			{
				HeartRateTrend = HeartRateTrends.None;
			}

			VRCOSC.SendHeartRates(heartRate);

			if (OSCSender == null || !ConfigData.OSCUseManualConfig) return;

			foreach (OSCParameter oscParameter in ConfigData.OSCParameters)
			{
				OscMessage? oscMessage = oscParameter.GetOscMessage(ConfigData.OSCPath, heartRate, HBToggle);
				if (oscMessage != null) OSCSender.Send(oscMessage);
			}
		}

		private static float CalcualteTrend(List<int> values)
		{
			int n = values.Count;
			int sumX = 0, sumY = 0, sumXY = 0, sumX2 = 0;

			for (int i = 0; i < n; i++)
			{
				sumX += i;
				sumY += values[i];
				sumXY += i * values[i];
				sumX2 += i * i;
			}

			return (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
		}

		private static void SetUI(string errorText = "", string indicatorColor = "#00000000", string bpmText = "", string measuredAtText = "")
		{
			MainViewModel.ErrorText = errorText;
			MainViewModel.ErrorTextColor = indicatorColor;
			MainViewModel.LiveIndicatorColor = indicatorColor;
			MainViewModel.BPMText = bpmText;
			MainViewModel.MeasuredAtText = measuredAtText;
		}
	}
}