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
using System.IO;
using System.Reflection;

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
		private const string ClientID = "ZTQ5ZDVhMGMtZWM0My00MDUzLTgyYTgtMmM1YzkxMzE5ZTNh";
		private static readonly int[] HttpPorts = [54269, 60422, 63671];
		private static HttpListener? _listenerHttpServer;
		private static bool _httpServerIsRunning = false;
		private static string _httpServerUri = string.Empty;


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
			bool responseModeWebPage = redirectUri == string.Empty;
			string baseUrl = "https://pulsoid.net/oauth2/authorize";
			string client_id = Encoding.UTF8.GetString(Convert.FromBase64String(ClientID));
			string redirect_uri = redirectUri;
			string response_type = "token";
			string scope = "data:heart_rate:read";
			string state = Guid.NewGuid().ToString();
			string response_mode = "web_page";

			string completeGetRequest =
				baseUrl
				+ "?client_id="		+ client_id
				+ "&redirect_uri="	+ (responseModeWebPage ? string.Empty : redirect_uri)
				+ "&response_type="	+ response_type
				+ "&scope="			+ scope
				+ "&state="			+ state
				+ (responseModeWebPage ? "&response_mode=" + response_mode : string.Empty)
			;

			return completeGetRequest;
		}

		public static void GetPulsoidToken()
		{
			string redirectUri = string.Empty;

			if (_httpServerIsRunning)
			{
				redirectUri = _httpServerUri;
			}
			else
			{
				foreach (int port in HttpPorts)
				{
					if (IsPortAvailable(port))
					{
						redirectUri = $"http://localhost:{port}/pulsoidtokenredirect/";

						StartGETServer(redirectUri);

						break;
					}
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
				string client_id = resultJson.ClientId ?? string.Empty;
				int expires_in = resultJson.ExpiresIn ?? 0;
				string profile_id = resultJson.ProfileId ?? string.Empty;
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

		private static void StartGETServer(string redirectUri)
		{
			if (!_httpServerIsRunning && redirectUri != string.Empty)
			{
				_httpServerIsRunning = true;
				_httpServerUri = redirectUri;
				Task.Run(async () =>
				{
					await StartHTTPServer(redirectUri);
				});
			}
		}

		public static void StopGETServer()
		{
			_httpServerIsRunning = false;
			_httpServerUri = string.Empty;
			_listenerHttpServer?.Stop();
			_listenerHttpServer?.Close();
			_listenerHttpServer = null;
		}

		private static async Task StartHTTPServer(string redirectUri)
		{
			_listenerHttpServer = new HttpListener();
			_listenerHttpServer.Prefixes.Add(redirectUri);
			_listenerHttpServer.Start();

			while (_httpServerIsRunning)
			{
				ProcessHTTPRequest(await _listenerHttpServer.GetContextAsync());
			}
		}

		private static void ProcessHTTPRequest(HttpListenerContext context)
		{
			bool tokenReceived = false;
			string responseString = HttpResponse.Get(HttpResponse.Responses.Error);
			
			if (context.Request.Url != null && context.Request.Url.ToString().Contains("/pulsoidtokenredirect/?"))
			{
				string queryString = context.Request.Url.Query;
				if (queryString.StartsWith('?')) queryString = queryString[1..];
				NameValueCollection queryParameters = HttpUtility.ParseQueryString(queryString);

				string token = queryParameters["token"] ?? string.Empty;
				string accessToken = queryParameters["access_token"] ?? string.Empty;
				if (!Int32.TryParse(queryParameters["expires_in"], out int expiresIn)) expiresIn = 0;
				string scope = queryParameters["scope"] ?? string.Empty;
				string state = queryParameters["state"] ?? string.Empty;

				if (MyRegex.GUID().IsMatch(accessToken) && scope.Contains("data:heart_rate:read") && expiresIn > 0)
				{
					SetPulsoidToken(accessToken);
					MainProgram.MainViewModel.OptionsViewModel.OptionsGeneralViewModel.TokenText = ConfigData.PulsoidToken;
					MainProgram.MainViewModel.OptionsViewModel.OptionsGeneralViewModel.TokenValidity = TokenValidity;

					Task.Run(async () =>
					{
						await ValidateToken();
						MainProgram.MainViewModel.OptionsViewModel.OptionsGeneralViewModel.TokenValidity = TokenValidity;
					});

					tokenReceived = true;
					responseString = HttpResponse.Get(HttpResponse.Responses.TokenObtained);
				}
			}
			else if (context.Request.Url != null && context.Request.Url.ToString().Contains("/pulsoidtokenredirect/"))
			{
				responseString = HttpResponse.Get(HttpResponse.Responses.Redirect);
			}

			HttpListenerResponse response = context.Response;
			byte[] buffer = Encoding.UTF8.GetBytes(responseString);

			response.ContentLength64 = buffer.Length;
			Stream output = response.OutputStream;
			output.Write(buffer, 0, buffer.Length);
			output.Close();

			if (tokenReceived) StopGETServer();
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

		private static class HttpResponse
		{
			private static readonly Assembly Assembly = Assembly.GetExecutingAssembly();
			private static readonly string ResourceRoot = "PulsoidToOSC.PulsoidHttpResponses";
			private static readonly Dictionary<Responses, string> ResourceNames = new()
			{
				{Responses.Error, "Error.html" },
				{Responses.Redirect, "Redirect.html" },
				{Responses.TokenObtained, "TokenObtained.html" }
			};

			public enum Responses { Error, Redirect, TokenObtained}

			public static string Get(Responses response)
			{
				return ReadResource($"{ResourceRoot}.{ResourceNames[response]}");
			}

			private static string ReadResource(string resourceName)
			{
				string result = string.Empty;

				using (Stream? stream = Assembly.GetManifestResourceStream(resourceName))
				{
					if (stream == null) return string.Empty;

					using (StreamReader reader = new(stream))
					{
						result = reader.ReadToEnd();
					}
				}

				return result;
			}
		}
	}
}