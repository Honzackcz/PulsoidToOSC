using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Net;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Collections.Specialized;
using System.Web;
using System.Net.Sockets;

namespace PulsoidToOSC
{
	internal static class PulsoidApi
	{
		public class Json
		{
			public class WSMessage
			{
				[JsonPropertyName("measured_at")]
				public long? MeasuredAt { get; set; }

				[JsonPropertyName("data")]
				public WSMessage_Data? Data { get; set; }
			}
			public class WSMessage_Data
			{
				[JsonPropertyName("heart_rate")]
				public int? HeartRate { get; set; }
			}
			public class ValidateResponse
			{
				[JsonPropertyName("client_id")]
				public string? ClientId { get; set; }

				[JsonPropertyName("expires_in")]
				public int? ExpiresIn { get; set; }

				[JsonPropertyName("profile_id")]
				public string? ProfileId { get; set; }

				[JsonPropertyName("scopes")]
				public List<string>? Scopes { get; set; }
			}
		}

		public enum TokenValidities { Invalid, Unknown, Valid };
		public static TokenValidities TokenValidity { get; set; } = TokenValidities.Unknown;
		public const string PulsoidWSURL = "wss://dev.pulsoid.net/api/v1/data/real_time?access_token=";
		private const string _clientID = "ZTQ5ZDVhMGMtZWM0My00MDUzLTgyYTgtMmM1YzkxMzE5ZTNh";
		private static readonly int[] httpPorts = [54269, 60422, 63671];


		public static void SetPulsoidToken(string? token)
		{
			if (token != null && token != ConfigData.PulsoidToken && MyRegex.GUID().IsMatch(token))
			{
				ConfigData.PulsoidToken = token;
				TokenValidity = TokenValidities.Unknown;
				ConfigData.SaveConfig();
			}
		}

		private static string PulsoidAuthorizeUrl(string redirectUri = "")
		{
			bool responseModeWebPage = redirectUri == "";
			string baseUrl = "https://pulsoid.net/oauth2/authorize";
			string client_id = Encoding.UTF8.GetString(Convert.FromBase64String(_clientID));
			string redirect_uri = redirectUri;
			string response_type = "token";
			string scope = "data:heart_rate:read";
			string state = Guid.NewGuid().ToString();
			string response_mode = "web_page";

			string completeGetRequest =
				baseUrl
				+ "?client_id="		+ client_id
				+ "&redirect_uri="	+ (responseModeWebPage ? "" : redirect_uri)
				+ "&response_type="	+ response_type
				+ "&scope="			+ scope
				+ "&state="			+ state
				+ (responseModeWebPage ? "&response_mode=" + response_mode : "")
			;

			return completeGetRequest;
		}

		public static void GetPulsoidToken()
		{
			string redirectUri = "";
			
			foreach (int port in httpPorts)
			{
				if (IsPortAvailable(port))
				{
					redirectUri = $"http://localhost:{port}/pulsoidtokenredirect/";

					StartGETServer(redirectUri);

					break;
				}
			}

			Process.Start(new ProcessStartInfo
			{
				FileName = PulsoidAuthorizeUrl(redirectUri),
				UseShellExecute = true
			});
		}

		public static async Task ValidateToken()
		{
			if (!MyRegex.GUID().IsMatch(ConfigData.PulsoidToken))
			{
				TokenValidity = TokenValidities.Invalid;
				return;
			}

			try
			{
				string result = await ValidateTokenAsync(ConfigData.PulsoidToken);
				
				Json.ValidateResponse? resultJson = JsonSerializer.Deserialize<Json.ValidateResponse>(result);

				if (resultJson == null) return;
				string client_id = resultJson.ClientId ?? "";
				int expires_in = resultJson.ExpiresIn ?? 0;
				string profile_id = resultJson.ProfileId ?? "";
				List<string> scopes = resultJson.Scopes ?? [];

				if (scopes.Contains("data:heart_rate:read") && expires_in > 0) TokenValidity = TokenValidities.Valid;
				else TokenValidity = TokenValidities.Invalid;
			}
			catch (Exception ex)
			{
				if (ex.Message.Contains(" 401 ")) TokenValidity = TokenValidities.Invalid;
				else TokenValidity = TokenValidities.Unknown;
			}
		}

		private static async Task<string> ValidateTokenAsync(string token)
		{
			using HttpClient client = new();
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
			client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

			HttpResponseMessage response = await client.GetAsync("https://dev.pulsoid.net/api/v1/token/validate");
			response.EnsureSuccessStatusCode();

			return await response.Content.ReadAsStringAsync();
		}


		// Experimental code for automatic token entry
		private static HttpListener? _listenerHttpServer;
		private static bool _httpServerIsRunning = false;

		public static void StartGETServer(string redirectUri)
		{
			if (!_httpServerIsRunning && redirectUri != "")
			{
				_httpServerIsRunning = true;
				Debug.WriteLine("Starting server...");
				Task.Run(async () =>
				{
					await StartHTTPServer(redirectUri);
				});
			}
		}

		private static async Task StartHTTPServer(string redirectUri)
		{
			_listenerHttpServer = new HttpListener();
			_listenerHttpServer.Prefixes.Add(redirectUri);
			_listenerHttpServer.Start();
			Debug.WriteLine("Server started. Listening on " + redirectUri);

			while (_httpServerIsRunning)
			{
				ProcessHTTPRequest(await _listenerHttpServer.GetContextAsync());
			}
		}

		private static void ProcessHTTPRequest(HttpListenerContext context)
		{
			bool tokenReceived = false;
			string responseString = "<!DOCTYPE html>\r\n<html lang=\"en\">\r\n<head>\r\n    <meta charset=\"UTF-8\">\r\n    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">\r\n    <title>Error</title>\r\n    <link rel=\"icon\" href=\"data:,\" />\r\n</head>\r\n<body>\r\n    <h1>Error</h1>\r\n</body>\r\n</html>";

			if (context.Request.Url != null && context.Request.Url.ToString().Contains("/pulsoidtokenredirect/?"))
			{
				string queryString = context.Request.Url.Query;
				if (queryString.StartsWith('?')) queryString = queryString[1..];
				NameValueCollection queryParameters = HttpUtility.ParseQueryString(queryString);

				string token = queryParameters["token"] ?? "";
				string accessToken = queryParameters["access_token"] ?? "";
				if (!Int32.TryParse(queryParameters["expires_in"], out int expiresIn)) expiresIn = 0;
				string scope = queryParameters["scope"] ?? "";
				string state = queryParameters["state"] ?? "";

				if (MyRegex.GUID().IsMatch(accessToken) && scope.Contains("data:heart_rate:read") && expiresIn > 0)
				{
					SetPulsoidToken(accessToken);
					MainProgram.MainViewModel.OptionsViewModel.GeneralOptionsViewModel.TokenText = ConfigData.PulsoidToken;
					_ = ValidateToken();

					tokenReceived = true;
					responseString = "<!DOCTYPE html>\r\n<html lang=\"en\">\r\n<head>\r\n    <meta charset=\"UTF-8\">\r\n    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">\r\n    <title>Token obtained</title>\r\n    <link rel=\"icon\" href=\"data:,\" />\r\n</head>\r\n<body>\r\n    <h1>Token obtained - you can now close this page</h1>\r\n</body>\r\n</html>";
				}
			}
			else if (context.Request.Url != null && context.Request.Url.ToString().Contains("/pulsoidtokenredirect/"))
			{
				responseString = "<!DOCTYPE html>\r\n<html lang=\"en\">\r\n<head>\r\n    <meta charset=\"UTF-8\">\r\n    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">\r\n    <title>Redirect</title>\r\n    <link rel=\"icon\" href=\"data:,\" />\r\n    <script>\r\n        document.addEventListener(\"DOMContentLoaded\", function() {\r\n            var currentUrl = window.location.href;\r\n            if (currentUrl.includes('#')) {\r\n                var newUrl = currentUrl.replace(/#/, '?');;\r\n                window.location.replace(newUrl);\r\n            }\r\n        });\r\n    </script>\r\n</head>\r\n<body>\r\n    <h1>Redirecting...</h1>\r\n</body>\r\n</html>";
			}

			HttpListenerResponse response = context.Response;
			byte[] buffer = Encoding.UTF8.GetBytes(responseString);

			response.ContentLength64 = buffer.Length;
			System.IO.Stream output = response.OutputStream;
			output.Write(buffer, 0, buffer.Length);
			output.Close();

			Debug.WriteLine($"Received request: {context.Request.Url}");
			if (tokenReceived) StopGETServer();
		}

		public static void StopGETServer()
		{
			_httpServerIsRunning = false;
			_listenerHttpServer?.Stop();
		}

		public static bool IsPortAvailable(int port)
		{
			bool isAvailable = true;
			TcpListener? tcpListener = null;

			try
			{
				tcpListener = new TcpListener(IPAddress.Loopback, port);
				tcpListener.Start();
			}
			catch (SocketException)
			{
				isAvailable = false;
			}
			finally
			{
				tcpListener?.Stop();
			}

			return isAvailable;
		}
	}
}