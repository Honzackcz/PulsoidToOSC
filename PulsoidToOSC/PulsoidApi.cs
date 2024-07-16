using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Net;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Collections.Specialized;
using System.Web;

namespace PulsoidToOSC
{
	internal static class PulsoidApi
	{
		public class JsonWSMessage
		{
			[JsonPropertyName("measured_at")]
			public long? MeasuredAt { get; set; }

			[JsonPropertyName("data")]
			public JsonWSMessage_Data? Data { get; set; }
		}
		public class JsonWSMessage_Data
		{
			[JsonPropertyName("heart_rate")]
			public int? HeartRate { get; set; }
		}

		public class JsonValidateResponse
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

		public enum TokenValidities { invalid, unknown, valid };
		public static TokenValidities tokenValiditi = TokenValidities.unknown;
		public static readonly bool responseModeWebPage = true;
		public const string pulsoidWSURL = "wss://dev.pulsoid.net/api/v1/data/real_time?access_token=";
		private const string client_ID = "";
		private static readonly string redirectUri = "http://localhost:54269/pulsoidtokenredirect/";

		public static void SetPulsoidToken(string? token)
		{
			if (token != null && token != ConfigData.PulsoidToken && MyRegex.RegexGUID().IsMatch(token))
			{
				ConfigData.PulsoidToken = token;
				tokenValiditi = TokenValidities.unknown;
				ConfigData.SaveConfig();
			}
		}

		private static string PulsoidAuthorizeUrl()
		{
			string baseUrl = "https://pulsoid.net/oauth2/authorize";
			string client_id = Encoding.UTF8.GetString(Convert.FromBase64String(client_ID));
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
			Process.Start(new ProcessStartInfo
			{
				FileName = PulsoidAuthorizeUrl(),
				UseShellExecute = true
			});
		}

		public static async Task ValidateToken()
		{
			if (!MyRegex.RegexGUID().IsMatch(ConfigData.PulsoidToken))
			{
				tokenValiditi = TokenValidities.invalid;
				return;
			}

			try
			{
				string result = await ValidateTokenAsync(ConfigData.PulsoidToken);
				
				JsonValidateResponse? resultJson = JsonSerializer.Deserialize<JsonValidateResponse>(result);

				if (resultJson == null) return;
				string client_id = resultJson.ClientId ?? "";
				int expires_in = resultJson.ExpiresIn ?? 0;
				string profile_id = resultJson.ProfileId ?? "";
				List<string> scopes = resultJson.Scopes ?? [];

				if (scopes.Contains("data:heart_rate:read") && expires_in > 0) tokenValiditi = TokenValidities.valid;
				else tokenValiditi = TokenValidities.invalid;
			}
			catch (Exception ex)
			{
				if (ex.Message.Contains(" 401 ")) tokenValiditi = TokenValidities.invalid;
				else tokenValiditi = TokenValidities.unknown;
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
		private static HttpListener? _listenerHTTPServer;
		private static bool HTTPServerIsRunning = false;

		public static void StartGETServer()
		{
			if (!HTTPServerIsRunning && redirectUri != "")
			{
				HTTPServerIsRunning = true;
				Debug.WriteLine("Starting server...");
				Task.Run(StartHTTPServer);
			}
		}

		private static async Task StartHTTPServer()
		{
			_listenerHTTPServer = new HttpListener();
			_listenerHTTPServer.Prefixes.Add(redirectUri);
			_listenerHTTPServer.Start();
			Debug.WriteLine("Server started. Listening on " + redirectUri);

			while (HTTPServerIsRunning)
			{
				var context = await _listenerHTTPServer.GetContextAsync();
				ProcessHTTPRequest(context);
			}
		}

		private static void ProcessHTTPRequest(HttpListenerContext context)
		{
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

				if (MyRegex.RegexGUID().IsMatch(accessToken) && scope.Contains("data:heart_rate:read") && expiresIn > 0)
				{
					SetPulsoidToken(accessToken);
					tokenValiditi = TokenValidities.valid;

					responseString = "<!DOCTYPE html>\r\n<html lang=\"en\">\r\n<head>\r\n    <meta charset=\"UTF-8\">\r\n    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">\r\n    <title>Token obtained</title>\r\n    <link rel=\"icon\" href=\"data:,\" />\r\n</head>\r\n<body>\r\n    <h1>Token obtained - you can now close this page</h1>\r\n</body>\r\n</html>";
				}
			}
			else if (context.Request.Url != null && context.Request.Url.ToString().Contains("/pulsoidtokenredirect/"))
			{
				responseString = "<!DOCTYPE html>\r\n<html lang=\"en\">\r\n<head>\r\n    <meta charset=\"UTF-8\">\r\n    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">\r\n    <title>Redirect</title>\r\n    <link rel=\"icon\" href=\"data:,\" />\r\n    <script>\r\n        document.addEventListener(\"DOMContentLoaded\", function() {\r\n            var currentUrl = window.location.href;\r\n            if (currentUrl.includes('#')) {\r\n                var newUrl = currentUrl.replace(/#/, '?');;\r\n                window.location.replace(newUrl);\r\n            }\r\n        });\r\n    </script>\r\n</head>\r\n<body>\r\n    <h1>Redirecting...</h1>\r\n</body>\r\n</html>";
			}

			var response = context.Response;
			byte[] buffer = Encoding.UTF8.GetBytes(responseString);

			response.ContentLength64 = buffer.Length;
			var output = response.OutputStream;
			output.Write(buffer, 0, buffer.Length);
			output.Close();

			Debug.WriteLine($"Received request: {context.Request.Url}");
		}

		public static void StopGETServer()
		{
			HTTPServerIsRunning = false;
			_listenerHTTPServer?.Stop();
		}
	}
}