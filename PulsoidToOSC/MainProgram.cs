using SharpOSC;
using System.Text.Json;
using System.Windows.Threading;
using System.Net.WebSockets;

namespace PulsoidToOSC
{
	public static class MainProgram
	{
		private const string ColorGreen = "#00FF00";
		private const string ColorRed = "#FF0000";
		private const string ColorYellow = "#FFFF00";
		private const string ColorCyan = "#00FFFF";

		private enum AppSates { stopped, starting, running, stopping };
		private static AppSates appSate;
		public static readonly Dispatcher disp = Dispatcher.CurrentDispatcher;
		public static readonly MainViewModel MainViewModel = new();

		public enum HeartRateTrends { none, stable, upward, downward, strongUpward, strongDownward };
		public static HeartRateTrends HeartRateTrend { get; private set; } = HeartRateTrends.none;
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

			if (!MyRegex.RegexGUID().IsMatch(ConfigData.PulsoidToken)) PulsoidApi.tokenValiditi = PulsoidApi.TokenValidities.invalid;

			MainWindow mainWindow = new()
			{
				DataContext = MainViewModel
			};
			MainViewModel.mainWindow = mainWindow;
			mainWindow.Show();

			SetupOSC();
			SetupWebSocketEvents();

			if (ConfigData.AutoStart)
			{
				SetUI("Auto start...", ColorYellow);

				_ = Task.Run(async () =>
				{
					await Task.Delay(500, delayedStartTaskCts.Token);
					if (appSate == AppSates.stopped) disp.Invoke(() => StartPulsoidToOSC());
				}, delayedStartTaskCts.Token);
			}
		}

		public static void StartPulsoidToOSC(bool reconnect = false)
		{
			if (!reconnect) failedWSConnectionAttempts = 0;
			delayedStartTaskCts.Cancel();

			if (appSate == AppSates.running)
			{
				_ = StopPulsoidToOSC();
				return;
			}

			if (appSate != AppSates.stopped) return;

			if (PulsoidApi.tokenValiditi == PulsoidApi.TokenValidities.invalid)
			{
				SetUI("Invalid Pulsoid token!\nIn options setup valid token.", ColorRed);
				return;
			}

			appSate = AppSates.starting;
			MainViewModel.StartButtonContent = " ";
			SetUI("Connecting to Pulsoid...", ColorYellow);
			HeartRateTrend = HeartRateTrends.none;
			recentHeartRates.Clear();
			SetupOSC();
			VRCOSC.Query.SetupQuerry();
			_ = StartWebSocket();
		}

		public static async Task StopPulsoidToOSC()
		{
			if (appSate == AppSates.stopped || appSate == AppSates.stopping) return;

			appSate = AppSates.stopping;
			MainViewModel.StartButtonContent = " ";
			SetUI("Closing connection to Pulsoid...", ColorYellow);
			VRCOSC.Query.StopQuerry();

			if (SimpleWSClient.ClientState == WebSocketState.Open)
			{
				await SimpleWSClient.CloseConnectionAsync();
			}

			SendHeartRates();

			appSate = AppSates.stopped;
			MainViewModel.StartButtonContent = "Start";
			SetUI();
		}

		public static async void RestartPulsoidToOSC()
		{
			if (appSate == AppSates.stopped || appSate == AppSates.stopping) return;
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
			SimpleWSClient.OnOpen += () => disp.Invoke(() => OnWSOpen());
			SimpleWSClient.OnMessage += (message) => disp.Invoke(() => OnWSMessage(message));
			SimpleWSClient.OnClose += (response) => disp.Invoke(() => OnWSClose(response));
		}

		private static async Task StartWebSocket()
		{
			await SimpleWSClient.OpenConnectionAsync(PulsoidApi.pulsoidWSURL + ConfigData.PulsoidToken);

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
			appSate = AppSates.running;
			MainViewModel.StartButtonContent = "Stop";

			PulsoidApi.tokenValiditi = PulsoidApi.TokenValidities.valid;
			failedWSConnectionAttempts = 0;

			SetUI("Waiting for heart rate...", ColorYellow);
		}

		private static void OnWSClose(SimpleWSClient.Response response)
		{
			_ = StopPulsoidToOSC();

			if (response.HttpStatusCode == 400 || response.HttpStatusCode == 401 || response.HttpStatusCode == 403) //Invalid token
			{
				PulsoidApi.tokenValiditi = PulsoidApi.TokenValidities.invalid;

				SetUI("Invalid Pulsoid token!\nIn options setup valid token.", ColorRed);
			}
			else if (response.WebSocketCloseStatusCode > 1000 && (appSate == AppSates.stopping || appSate == AppSates.stopped) && SimpleWSClient.ClientState != WebSocketState.Open && SimpleWSClient.ClientState != WebSocketState.Connecting) //Connection lost
			{
				SetUI("Error: Connection to Pulsoid!", ColorRed);

				if (failedWSConnectionAttempts < 5 && PulsoidApi.tokenValiditi != PulsoidApi.TokenValidities.invalid)
				{
					SetUI($"Error: Connection to Pulsoid!\nRetrying connection... ({failedWSConnectionAttempts + 1})", ColorRed);

					delayedStartTaskCts = new();
					Task.Run(async () =>
					{
						await Task.Delay(failedWSConnectionAttempts * 10000, delayedStartTaskCts.Token);
						if (appSate == AppSates.stopped) disp.Invoke(() => StartPulsoidToOSC(true));
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

			PulsoidApi.JsonWSMessage? messageJson = JsonSerializer.Deserialize<PulsoidApi.JsonWSMessage>(message);

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
					> 1f => HeartRateTrends.strongUpward,
					< -1f => HeartRateTrends.strongDownward,
					> 0.5f => HeartRateTrends.upward,
					< -0.5f => HeartRateTrends.downward,
					_ => HeartRateTrends.stable
				};
			}
			else
			{
				HeartRateTrend = HeartRateTrends.none;
			}

			VRCOSC.SendHeartRates(heartRate);

			if (OSCSender == null || !ConfigData.OSCUseManualConfig) return;

			List<OscMessage> oscMessages = [
				new(ConfigData.OSCPath + "Heartrate", (heartRate / 127f) - 1f),			//Float ([0, 255] -> [-1, 1])
				new(ConfigData.OSCPath + "HeartRateFloat", (heartRate / 127f) - 1f),	//Float ([0, 255] -> [-1, 1])
				new(ConfigData.OSCPath + "Heartrate2", heartRate / 255f),				//Float ([0, 255] -> [0, 1]) 
				new(ConfigData.OSCPath + "HeartRateFloat01", heartRate / 255f),			//Float ([0, 255] -> [0, 1]) 
				new(ConfigData.OSCPath + "Heartrate3", heartRate),						//Int [0, 255]
				new(ConfigData.OSCPath + "HeartRateInt", heartRate),					//Int [0, 255]
				new(ConfigData.OSCPath + "HeartBeatToggle", HBToggle)                   //Bool reverses with each update
			];

			foreach (OscMessage message in oscMessages) if (message != null) OSCSender.Send(message);
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
