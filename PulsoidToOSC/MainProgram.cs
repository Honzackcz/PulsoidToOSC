using SharpOSC;
using System.Text.Json;
using System.Windows.Threading;
using System.Net.WebSockets;

namespace PulsoidToOSC
{
	internal static class MainProgram
	{
		public const string AppVersion = "v0.1.1";
		public const string GitHubOwner = "Honzackcz";
		public const string GitHubRepo = "PulsoidToOSC";

		public static Dispatcher Disp { get; } = Dispatcher.CurrentDispatcher;
		public static MainViewModel MainViewModel { get; } = new();
		public static UDPSender? OSCSender { get; private set; }
		public static bool HBToggle { get; private set; } = false;
		public enum HeartRateTrends { None, Stable, Upward, Downward, StrongUpward, StrongDownward };
		public static HeartRateTrends HeartRateTrend { get; private set; } = HeartRateTrends.None;

		private enum AppSates { Stopped, Starting, Running, Stopping };
		private static AppSates _appSate;
		private static readonly List<int> _recentHeartRates = [];
		private static DateTime _lastWSMessageTime = DateTime.MinValue;
		private static bool _wsMessageTimeout = false;
		private static int _failedWSConnectionAttempts = 0;
		private static CancellationTokenSource _delayedStartTaskCts = new();

		public static void StartUp()
		{
			ConfigData.LoadConfig();

			if (!MyRegex.GUID().IsMatch(ConfigData.PulsoidToken)) PulsoidApi.TokenValidity = PulsoidApi.TokenValidities.Invalid;

			MainViewModel.OpenMainWindow();

			SetupOSC();
			SetupWebSocketEvents();

			if (ConfigData.AutoStart)
			{
				MainViewModel.SetUI("Auto start...", MainViewModel.Colors.Yellow);

				_ = Task.Run(async () =>
				{
					await Task.Delay(500, _delayedStartTaskCts.Token);
					if (_appSate == AppSates.Stopped) Disp.Invoke(() => StartPulsoidToOSC());
				}, _delayedStartTaskCts.Token);
			}

			_ = Task.Run(async () => 
			{
				MainViewModel.InfoViewModel.IsNewVersionAvailable = await GitHubApi.IsNewVersionAvailable(GitHubOwner, GitHubRepo, AppVersion) == GitHubApi.VersionStatus.NewIsAvailable;
			});
		}

		public static void StartPulsoidToOSC(bool reconnect = false)
		{
			if (!reconnect) _failedWSConnectionAttempts = 0;
			_delayedStartTaskCts.Cancel();

			if (_appSate == AppSates.Running)
			{
				_ = StopPulsoidToOSC();
				return;
			}

			if (_appSate != AppSates.Stopped) return;

			if (PulsoidApi.TokenValidity == PulsoidApi.TokenValidities.Invalid)
			{
				MainViewModel.SetUI("Invalid Pulsoid token!\nIn options setup valid token.", MainViewModel.Colors.Red);
				return;
			}

			_appSate = AppSates.Starting;
			MainViewModel.StartButtonContent = " ";
			MainViewModel.SetUI("Connecting to Pulsoid...", MainViewModel.Colors.Yellow);
			HeartRateTrend = HeartRateTrends.None;
			_recentHeartRates.Clear();
			SetupOSC();
			VRCOSC.Query.SetupQuerry();
			_ = StartWebSocket();
		}

		public static async Task StopPulsoidToOSC()
		{
			if (_appSate == AppSates.Stopped || _appSate == AppSates.Stopping) return;

			_appSate = AppSates.Stopping;
			MainViewModel.StartButtonContent = " ";
			MainViewModel.SetUI("Closing connection to Pulsoid...", MainViewModel.Colors.Yellow);
			VRCOSC.Query.StopQuerry();

			if (SimpleWSClient.ClientState == WebSocketState.Open)
			{
				await SimpleWSClient.CloseConnectionAsync();
			}

			SendHeartRates();

			_appSate = AppSates.Stopped;
			MainViewModel.StartButtonContent = "Start";
			MainViewModel.SetUI();
		}

		public static async void RestartPulsoidToOSC()
		{
			if (_appSate == AppSates.Stopped || _appSate == AppSates.Stopping) return;
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

			_lastWSMessageTime = DateTime.MinValue;

			await Task.Delay(1000);
			while (SimpleWSClient.ClientState == WebSocketState.Open)
			{
				if (!_wsMessageTimeout && _lastWSMessageTime != DateTime.MinValue && _lastWSMessageTime.AddSeconds(10) < DateTime.UtcNow)
				{
					_wsMessageTimeout = true;
					SendHeartRates();

					MainViewModel.SetUI("Waiting for heart rate...", MainViewModel.Colors.Yellow);
				}
				await Task.Delay(100);
			}
		}

		private static void OnWSOpen()
		{
			_appSate = AppSates.Running;
			MainViewModel.StartButtonContent = "Stop";

			PulsoidApi.TokenValidity = PulsoidApi.TokenValidities.Valid;
			_failedWSConnectionAttempts = 0;

			MainViewModel.SetUI("Waiting for heart rate...", MainViewModel.Colors.Yellow);
		}

		private static void OnWSClose(SimpleWSClient.Response response)
		{
			_ = StopPulsoidToOSC();

			if (response.HttpStatusCode == 400 || response.HttpStatusCode == 401 || response.HttpStatusCode == 403) //Invalid token
			{
				PulsoidApi.TokenValidity = PulsoidApi.TokenValidities.Invalid;

				MainViewModel.SetUI("Invalid Pulsoid token!\nIn options setup valid token.", MainViewModel.Colors.Red);
			}
			else if (response.WebSocketCloseStatusCode > 1000 && (_appSate == AppSates.Stopping || _appSate == AppSates.Stopped) && SimpleWSClient.ClientState != WebSocketState.Open && SimpleWSClient.ClientState != WebSocketState.Connecting) //Connection lost
			{
				MainViewModel.SetUI("Error: Connection to Pulsoid!", MainViewModel.Colors.Red);

				if (_failedWSConnectionAttempts <= 20 && PulsoidApi.TokenValidity != PulsoidApi.TokenValidities.Invalid)
				{
					MainViewModel.SetUI($"Error: Connection to Pulsoid!\nRetrying connection... ({_failedWSConnectionAttempts + 1})", MainViewModel.Colors.Red);

					_delayedStartTaskCts = new();
					Task.Run(async () =>
					{
						await Task.Delay(5000 * Convert.ToInt32(_failedWSConnectionAttempts > 0), _delayedStartTaskCts.Token);
						if (_appSate == AppSates.Stopped) Disp.Invoke(() => StartPulsoidToOSC(true));
					}, _delayedStartTaskCts.Token);

					_failedWSConnectionAttempts++;
				}
			}
		}

		private static void OnWSMessage(string message)
		{
			_lastWSMessageTime = DateTime.UtcNow;
			_wsMessageTimeout = false;

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

			if (heartRate > 0) MainViewModel.SetUI("", HBToggle ? MainViewModel.Colors.Cyan : MainViewModel.Colors.Green, $"BPM: {heartRate}", measuredAt > 0 ? "Measured at: " + DateTimeOffset.FromUnixTimeMilliseconds(measuredAt).LocalDateTime.ToLongTimeString() : "");
			else MainViewModel.SetUI("Error at obtaing heart rate data!", MainViewModel.Colors.Red);
		}

		private static void SendHeartRates(int heartRate = 0)
		{
			if (heartRate < 0) return;

			HBToggle = !HBToggle;

			_recentHeartRates.Add(heartRate);
			if (_recentHeartRates.Count > 5)
			{
				_recentHeartRates.RemoveAt(0);
				HeartRateTrend = CalcualteTrend(_recentHeartRates) switch
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
	}
}