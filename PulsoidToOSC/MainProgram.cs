using SharpOSC;
using System.Net.WebSockets;

namespace PulsoidToOSC
{
	internal static class MainProgram
	{
		public const string AppVersion = "v0.3.1";
		public const string GitHubOwner = "Honzackcz";
		public const string GitHubRepo = "PulsoidToOSC";

		public static MainViewModel MainViewModel { get; } = new();
		public static UDPSender? OSCSender { get; private set; }

		private enum AppSates { Stopped, Starting, Running, Stopping };
		private static AppSates _appSate;
		private static DateTime _lastWSMessageTime = DateTime.MinValue;
		private static bool _wsMessageTimeout = false;
		private static int _failedWSConnectionAttempts = 0;
		private static bool _heartRateDataTimeoutRunning = false;
		private static CancellationTokenSource _delayedStartTaskCts = new();
		private static CancellationTokenSource _heartRateDataTimeoutCts = new();
		private static TaskCompletionSource<bool> _awaitWSConnectionLost = new();

		public static void StartUp()
		{
			ConfigData.LoadConfig();

			if (!MyRegex.GUID().IsMatch(ConfigData.PulsoidToken)) PulsoidApi.TokenValidity = PulsoidApi.TokenValidityStatus.Invalid;

			MainViewModel.OpenMainWindow();

			SetupOSC();
			SetupWebSocketEvents();

			if (ConfigData.AutoStart && PulsoidApi.TokenValidity != PulsoidApi.TokenValidityStatus.Invalid)
			{
				MainViewModel.SetWarning("Auto start...");

				_ = Task.Run(async () =>
				{
					await Task.Delay(500, _delayedStartTaskCts.Token);
					if (_appSate == AppSates.Stopped) StartPulsoidToOSC();
				}, _delayedStartTaskCts.Token);
			}

			_ = Task.Run(async () =>
			{
				MainViewModel.InfoViewModel.IsNewVersionAvailable = await GitHubApi.IsNewVersionAvailable(GitHubOwner, GitHubRepo, AppVersion) == GitHubApi.VersionStatus.NewIsAvailable;
			});
		}

		public static async void StartPulsoidToOSC()
		{
			_failedWSConnectionAttempts = 0;

			while (true)
			{
			_delayedStartTaskCts.Cancel();

			if (_appSate == AppSates.Running)
			{
				_ = StopPulsoidToOSC();
				return;
			}

			if (_appSate != AppSates.Stopped) return;

				if (PulsoidApi.TokenValidity == PulsoidApi.TokenValidityStatus.Invalid)
			{
				MainViewModel.SetError("Invalid Pulsoid token!\nIn options setup valid token.");
				return;
			}

			_appSate = AppSates.Starting;
			MainViewModel.StartButton = MainViewModel.StartButtonType.Disabled;
			MainViewModel.SetWarning("Connecting to Pulsoid...");
			HeartRate.Reset();
			SetupOSC();
				VRCOSC.Query.SetupQuery();

				_awaitWSConnectionLost = new();
			StartWebSocket();
				bool retry = await _awaitWSConnectionLost.Task;

				if (retry)
				{
					_ = StopPulsoidToOSC();

					if (_failedWSConnectionAttempts <= 20)
					{
						MainViewModel.SetError($"Error: Connection to Pulsoid!\nRetrying connection... ({_failedWSConnectionAttempts})");

						_delayedStartTaskCts = new();
						try
						{
							await Task.Delay(_failedWSConnectionAttempts == 0 ? 0 : 5000, _delayedStartTaskCts.Token);
						}
						catch
						{
							return;
						}
						_failedWSConnectionAttempts++;
					}
					else
					{
						return;
					}
				}
				else
				{
					return;
				}
			}
		}

		public static async Task StopPulsoidToOSC()
		{
			if (_appSate == AppSates.Stopped || _appSate == AppSates.Stopping) return;
			_appSate = AppSates.Stopping;

			_awaitWSConnectionLost.TrySetResult(false);
			if(_heartRateDataTimeoutRunning) _heartRateDataTimeoutCts.Cancel();

			MainViewModel.StartButton = MainViewModel.StartButtonType.Disabled;

			if (SimpleWSClient.ClientState == WebSocketState.Open)
			{
				MainViewModel.SetWarning("Closing connection to Pulsoid...");
				await SimpleWSClient.CloseConnectionAsync();
			}

			HeartRate.Send();
			VRCOSC.Query.StopQuery();

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
			SimpleWSClient.OnOpen += OnWSOpen;
			SimpleWSClient.OnMessage += OnWSMessage;
			SimpleWSClient.OnClose += OnWSClose;
		}

		private static async void StartWebSocket()
		{
			await SimpleWSClient.OpenConnectionAsync(PulsoidApi.PulsoidWsUri + ConfigData.PulsoidToken);
		}

		private static async void HeartRateDataTimeout()
		{
			if (_heartRateDataTimeoutRunning) return;

			_lastWSMessageTime = DateTime.MinValue;
			_heartRateDataTimeoutRunning = true;
			_heartRateDataTimeoutCts = new();

			while (SimpleWSClient.ClientState == WebSocketState.Open)
			{
				if (!_wsMessageTimeout && _lastWSMessageTime != DateTime.MinValue && _lastWSMessageTime.AddSeconds(10) < DateTime.UtcNow)
				{
					_wsMessageTimeout = true;
					HeartRate.Send();

					MainViewModel.SetWarning("Waiting for heart rate...");
				}

				try
				{
					await Task.Delay(100, _heartRateDataTimeoutCts.Token);
			}
				catch { }
			}

			_heartRateDataTimeoutRunning = false;
		}

		private static void OnWSOpen()
		{
			_appSate = AppSates.Running;
			MainViewModel.StartButton = MainViewModel.StartButtonType.Stop;

			PulsoidApi.TokenValidity = PulsoidApi.TokenValidityStatus.Valid;
			_failedWSConnectionAttempts = 0;

			MainViewModel.SetWarning("Waiting for heart rate...");

			HeartRateDataTimeout();
		}

		private static void OnWSClose(SimpleWSClient.Response response)
		{

			if (response.HttpStatusCode == 400 || response.HttpStatusCode == 401 || response.HttpStatusCode == 403) //Invalid token
			{
				_ = StopPulsoidToOSC();

				PulsoidApi.TokenValidity = PulsoidApi.TokenValidityStatus.Invalid;

				MainViewModel.SetError("Invalid Pulsoid token!\nIn options setup valid token.");
			}
			else if (response.WebSocketCloseStatusCode > 1000 && SimpleWSClient.ClientState != WebSocketState.Open && SimpleWSClient.ClientState != WebSocketState.Connecting) //Connection lost
			{
				MainViewModel.SetError("Error: Connection to Pulsoid!");

				_awaitWSConnectionLost.TrySetResult(true);
			}
			else
				{
				_ = StopPulsoidToOSC();
			}
		}

		private static void OnWSMessage(string message)
		{
			_lastWSMessageTime = DateTime.UtcNow;
			_wsMessageTimeout = false;

			bool hrSucceed = PulsoidApi.ProcessWSMessage(message, out long measuredAt, out int heartRate);

			HeartRate.Send(heartRate);

			if (HeartRate.HRValue > 0 && hrSucceed) MainViewModel.SetRunning($"BPM: {HeartRate.HRValue}", measuredAt > 0 ? "Measured at: " + DateTimeOffset.FromUnixTimeMilliseconds(measuredAt).LocalDateTime.ToLongTimeString() : string.Empty);
			else MainViewModel.SetError("Error at obtaining heart rate data!");
		}
	}
}