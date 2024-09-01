using SharpOSC;
using System.Text.Json;
using System.Windows.Threading;
using System.Net.WebSockets;

namespace PulsoidToOSC
{
	internal static class MainProgram
	{
		public const string AppVersion = "v0.2.0";
		public const string GitHubOwner = "Honzackcz";
		public const string GitHubRepo = "PulsoidToOSC";

		public static Dispatcher Disp { get; } = Dispatcher.CurrentDispatcher;
		public static MainViewModel MainViewModel { get; } = new();
		public static UDPSender? OSCSender { get; private set; }

		private enum AppSates { Stopped, Starting, Running, Stopping };
		private static AppSates _appSate;
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
				MainViewModel.SetWarning("Auto start...");

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
				MainViewModel.SetError("Invalid Pulsoid token!\nIn options setup valid token.");
				return;
			}

			_appSate = AppSates.Starting;
			MainViewModel.StartButton = MainViewModel.StartButtonType.Disabled;
			MainViewModel.SetWarning("Connecting to Pulsoid...");
			HeartRate.ResetTrends();
			SetupOSC();
			VRCOSC.Query.SetupQuerry();
			_ = StartWebSocket();
		}

		public static async Task StopPulsoidToOSC()
		{
			if (_appSate == AppSates.Stopped || _appSate == AppSates.Stopping) return;

			_appSate = AppSates.Stopping;
			MainViewModel.StartButton = MainViewModel.StartButtonType.Disabled;
			MainViewModel.SetWarning("Closing connection to Pulsoid...");
			VRCOSC.Query.StopQuerry();

			if (SimpleWSClient.ClientState == WebSocketState.Open)
			{
				await SimpleWSClient.CloseConnectionAsync();
			}

			HeartRate.Send();

			_appSate = AppSates.Stopped;
			MainViewModel.StartButton = MainViewModel.StartButtonType.Start;
			MainViewModel.ClearUI();
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
					HeartRate.Send();

					MainViewModel.SetWarning("Waiting for heart rate...");
				}
				await Task.Delay(100);
			}
		}

		private static void OnWSOpen()
		{
			_appSate = AppSates.Running;
			MainViewModel.StartButton = MainViewModel.StartButtonType.Stop;

			PulsoidApi.TokenValidity = PulsoidApi.TokenValidities.Valid;
			_failedWSConnectionAttempts = 0;

			MainViewModel.SetWarning("Waiting for heart rate...");
		}

		private static void OnWSClose(SimpleWSClient.Response response)
		{
			_ = StopPulsoidToOSC();

			if (response.HttpStatusCode == 400 || response.HttpStatusCode == 401 || response.HttpStatusCode == 403) //Invalid token
			{
				PulsoidApi.TokenValidity = PulsoidApi.TokenValidities.Invalid;

				MainViewModel.SetError("Invalid Pulsoid token!\nIn options setup valid token.");
			}
			else if (response.WebSocketCloseStatusCode > 1000 && (_appSate == AppSates.Stopping || _appSate == AppSates.Stopped) && SimpleWSClient.ClientState != WebSocketState.Open && SimpleWSClient.ClientState != WebSocketState.Connecting) //Connection lost
			{
				MainViewModel.SetError("Error: Connection to Pulsoid!");

				if (_failedWSConnectionAttempts <= 20 && PulsoidApi.TokenValidity != PulsoidApi.TokenValidities.Invalid)
				{
					MainViewModel.SetError($"Error: Connection to Pulsoid!\nRetrying connection... ({_failedWSConnectionAttempts + 1})");

					_delayedStartTaskCts = new();
					Task.Run(async () =>
					{
						await Task.Delay(_failedWSConnectionAttempts == 0 ? 0 : 5000, _delayedStartTaskCts.Token);
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

			HeartRate.Send(heartRate);

			if (heartRate > 0) MainViewModel.SetRunning($"BPM: {heartRate}", measuredAt > 0 ? "Measured at: " + DateTimeOffset.FromUnixTimeMilliseconds(measuredAt).LocalDateTime.ToLongTimeString() : string.Empty);
			else MainViewModel.SetError("Error at obtaing heart rate data!");
		}
	}
}