using SharpOSC;
using System.Net.WebSockets;

namespace PulsoidToOSC
{
	internal static class MainProgram
	{
		public const string AppVersion = "v0.6.0";
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
			if (_heartRateDataTimeoutRunning) _heartRateDataTimeoutCts.Cancel();
			if (TestHeartRate.Running) TestHeartRate.Cts.Cancel();

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


		internal static class TestHeartRate
		{
			public static bool Running { get => _testHeartRateRunning; }
			public static CancellationTokenSource Cts { get => _testHeartRateCts; }
			public static int MinHeartRate { get => _minHeartRate; set => _minHeartRate = Math.Clamp(value, 1, _maxHeartRate); }
			public static int MaxHeartRate { get => _maxHeartRate; set => _maxHeartRate = Math.Clamp(value, _minHeartRate, 255); }
			public static int IncrementStep { get => _incrementStep; set => _incrementStep = Math.Clamp(value, 1, 10); }
			public static int IncrementInterval { get => _incrementInterval; set => _incrementInterval = Math.Clamp(value, 1, 100); }

			private static bool _testHeartRateRunning = false;
			private static CancellationTokenSource _testHeartRateCts = new();
			private static int _minHeartRate = 60;
			private static int _maxHeartRate = 120;
			private static int _incrementStep = 1;
			private static int _incrementInterval = 10;

			public static async void Start()
			{
				if (_appSate == AppSates.Running && _testHeartRateRunning)
				{
					_ = StopPulsoidToOSC();
					return;
				}
				if (_appSate != AppSates.Stopped) return;

				_appSate = AppSates.Starting;
				MainViewModel.StartButton = MainViewModel.StartButtonType.Disabled;
				MainViewModel.SetWarning("Starting heart rate test...");
				HeartRate.Reset();
				SetupOSC();
				VRCOSC.Query.SetupQuery();

				_testHeartRateCts = new();
				_testHeartRateRunning = true;
				_appSate = AppSates.Running;
				MainViewModel.StartButton = MainViewModel.StartButtonType.Stop;

				int heartRate = Math.Clamp(80, _minHeartRate, _maxHeartRate);
				bool increasing = true;

				while (_appSate == AppSates.Running && _testHeartRateRunning && !_testHeartRateCts.IsCancellationRequested)
				{
					//{"measured_at": 1625310655000, "data": {"heart_rate": 40}}"
					OnWSMessage("{\"measured_at\": " + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + ", \"data\": {\"heart_rate\": " + heartRate + "}}");

					if (increasing)
					{
						heartRate = Math.Clamp(heartRate + _incrementStep, _minHeartRate, _maxHeartRate);
						if (heartRate >= _maxHeartRate) increasing = false;
					}
					else
					{
						heartRate = Math.Clamp(heartRate - _incrementStep, _minHeartRate, _maxHeartRate);
						if (heartRate <= _minHeartRate) increasing = true;
					}

					try
					{
						await Task.Delay(_incrementInterval * 100, _testHeartRateCts.Token);
					}
					catch
					{
						break;
					}
				}

				_testHeartRateRunning = false;
			}
		}
	}
}