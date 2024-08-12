using System.Net.WebSockets;
using System.Text;

namespace PulsoidToOSC
{
    internal class SimpleWSClient
    {
		public class Response
		{
			public int WebSocketCloseStatusCode { get; set; }
			public int HttpStatusCode { get; set;}
		}

		public delegate void OnMessageEventHandler(string message);
		public delegate void OnCloseEventHandler(Response response);
		public delegate void OnOpenEventHandler();
		public static event OnMessageEventHandler? OnMessage;
		public static event OnCloseEventHandler? OnClose;
		public static event OnOpenEventHandler? OnOpen;

		private static ClientWebSocket _wsClient = new();

		public static WebSocketState ClientState 
		{
			get => _wsClient.State;
		}

		public static async Task OpenConnectionAsync(string Url)
		{
			if (_wsClient.State == WebSocketState.Aborted || _wsClient.State == WebSocketState.Closed || _wsClient.State == WebSocketState.None)
			{
				_wsClient = new();
				_wsClient.Options.KeepAliveInterval = TimeSpan.FromSeconds(10);
				_wsClient.Options.CollectHttpResponseDetails = true;

				try
				{
					await _wsClient.ConnectAsync(new Uri(Url), CancellationToken.None);
					Opened();
					_ = HandleMessagesAsync();
				}
				catch
				{
					Closed();
					return;
				}
			}
		}

		public static async Task CloseConnectionAsync()
		{
			await _wsClient.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
		}

		private static async Task HandleMessagesAsync()
		{
			var buffer = new byte[1024 * 4];
			WebSocketException? webSocketException = null;

			while (_wsClient.State == WebSocketState.Open)
			{
				try
				{
					WebSocketReceiveResult result = await _wsClient.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None).ConfigureAwait(false);

					if (result.MessageType == WebSocketMessageType.Text && result.EndOfMessage)
					{
						string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
						if (message != string.Empty) Messaged(message);
					}
				}
				catch (WebSocketException ex)
				{
					webSocketException = ex;
				}
			}
			if (_wsClient.State != WebSocketState.Open)
			{
				Closed(webSocketException);
			}
		}

		private static void Messaged(string message)
		{
			if (_wsClient.State != WebSocketState.Open) return;
			OnMessage?.Invoke(message);
		}

		private static void Opened()
		{
			OnOpen?.Invoke();
		}

		private static void Closed(WebSocketException? ex = null)
		{
            int httpStatusCode = (int) _wsClient.HttpStatusCode;
			int webSocketCloseStatusCode = (int) (_wsClient.CloseStatus ?? 
				((ex != null && ex.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely) ?
				WebSocketCloseStatus.EndpointUnavailable :
				WebSocketCloseStatus.Empty));

			Response response = new()
			{
				HttpStatusCode = httpStatusCode,
				WebSocketCloseStatusCode = webSocketCloseStatusCode
			};

			OnClose?.Invoke(response);
		}
	}
}