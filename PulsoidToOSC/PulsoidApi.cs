using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

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

			public class ValidateTokenResponse
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

			public class DeviceAuthorizationFlow
			{
				public class InitialResponse
				{
					[JsonPropertyName("device_code")]
					public string? DeviceCode { get; set; }

					[JsonPropertyName("user_code")]
					public string? UserCode { get; set; }

					[JsonPropertyName("verification_uri")]
					public string? VerificationUri { get; set; }

					[JsonPropertyName("verification_uri_complete")]
					public string? VerificationUriComplete { get; set; }

					[JsonPropertyName("expires_in")]
					public int ExpiresIn { get; set; }

					[JsonPropertyName("interval")]
					public int Interval { get; set; }
				}
				public class TokenOKResponse
				{
					[JsonPropertyName("access_token")]
					public string? AccessToken { get; set; }

					[JsonPropertyName("expires_in")]
					public int ExpiresIn { get; set; }

					[JsonPropertyName("token_type")]
					public string? TokenType { get; set; }
				}
				public class TokenErrorResponse
				{
					[JsonPropertyName("error")]
					public string? Error { get; set; }

					[JsonPropertyName("error_description")]
					public string? ErrorDescription { get; set; }
				}
			}
		}

		public static bool ProcessWSMessage(string message, out long measuredAt, out int heartRate)
		{
			measuredAt = 0L;
			heartRate = 0;

			try
			{
				Json.WSMessage? messageJson = JsonSerializer.Deserialize<Json.WSMessage>(message);
				if (messageJson != null)
				{
					measuredAt = messageJson.MeasuredAt ?? 0L;
					if (messageJson.Data != null)
					{
						heartRate = messageJson.Data.HeartRate ?? 0;
					}
				}
				return true;
			}
			catch
			{
				return false;
			}
		}

		public enum TokenValidityStatus { Invalid, Unknown, Valid };
		public static TokenValidityStatus TokenValidity { get; set; } = TokenValidityStatus.Unknown;
		public const string PulsoidWsUri = "wss://dev.pulsoid.net/api/v1/data/real_time?access_token=";
		private const string PublicClientID = "ZTQ5ZDVhMGMtZWM0My00MDUzLTgyYTgtMmM1YzkxMzE5ZTNh";

		public static void SetPulsoidToken(string? token, bool saveConfig = true)
		{
			if (token != null && token != ConfigData.PulsoidToken && MyRegex.GUID().IsMatch(token))
			{
				ConfigData.PulsoidToken = token;
				TokenValidity = TokenValidityStatus.Unknown;
				if (saveConfig) ConfigData.SaveConfig();
			}
		}

		public static async Task GetPulsoidToken_DeviceAuthorizationFlow()
		{
			Json.DeviceAuthorizationFlow.InitialResponse? initialResponse = await InitiateDeviceAuthorizationSession();

			bool invalidVerificationUri = initialResponse == null || string.IsNullOrEmpty(initialResponse.VerificationUriComplete);

			Process.Start(new ProcessStartInfo
			{
				FileName = invalidVerificationUri ? GetManualAuthorizationUri() : initialResponse?.VerificationUriComplete,
				UseShellExecute = true
			});

			if (invalidVerificationUri) return;

			TokenValidity = TokenValidityStatus.Unknown;
			MainProgram.MainViewModel.OptionsViewModel.OptionsGeneralViewModel.TokenValidity = TokenValidity;

			DateTime expireTime = DateTime.UtcNow.AddSeconds(initialResponse?.ExpiresIn ?? 0);
			const string pollingUri = "https://pulsoid.net/oauth2/token";
			using HttpClient httpClient = new();
			FormUrlEncodedContent formData = new(
			[
				new KeyValuePair<string, string>("grant_type", "urn:ietf:params:oauth:grant-type:device_code"),
				new KeyValuePair<string, string>("device_code", initialResponse?.DeviceCode ?? ""),
				new KeyValuePair<string, string>("client_id", Encoding.UTF8.GetString(Convert.FromBase64String(PublicClientID)))
			]);

			while (true)
			{
				await Task.Delay(1000 * (initialResponse?.Interval ?? 3));
				if (DateTime.UtcNow > expireTime) break;

				HttpResponseMessage? httpResponse;

				try
				{
					httpResponse = await httpClient.PostAsync(pollingUri, formData);
				}
				catch
				{
					continue;
				}

				if (httpResponse == null) continue;
				string httpResponseBody = await httpResponse.Content.ReadAsStringAsync();

				if (httpResponse.StatusCode == HttpStatusCode.OK)
				{
					try
					{
						Json.DeviceAuthorizationFlow.TokenOKResponse? tokenOKResponse = JsonSerializer.Deserialize<Json.DeviceAuthorizationFlow.TokenOKResponse>(httpResponseBody);
						if (tokenOKResponse == null) break;
						if (MyRegex.GUID().IsMatch(tokenOKResponse.AccessToken ?? "") && tokenOKResponse.ExpiresIn > 0)
						{
							SetPulsoidToken(tokenOKResponse.AccessToken);
							MainProgram.MainViewModel.OptionsViewModel.OptionsGeneralViewModel.TokenText = ConfigData.PulsoidToken;
							MainProgram.MainViewModel.OptionsViewModel.OptionsGeneralViewModel.TokenValidity = TokenValidity;
						}
					}
					catch
					{
						
					}
					break;
				}
				else if (httpResponse.StatusCode == HttpStatusCode.BadRequest)
				{
					try
					{
						Json.DeviceAuthorizationFlow.TokenErrorResponse? tokenErrorResponse = JsonSerializer.Deserialize<Json.DeviceAuthorizationFlow.TokenErrorResponse>(httpResponseBody);
						if (tokenErrorResponse?.Error != "authorization_pending")
						{
							break;
						}
					}
					catch
					{
						continue;
					}
				}
			}

			await ValidateToken();
			MainProgram.MainViewModel.OptionsViewModel.OptionsGeneralViewModel.TokenValidity = TokenValidity;
		}

		private static async Task<Json.DeviceAuthorizationFlow.InitialResponse?> InitiateDeviceAuthorizationSession()
		{
			const string initiateUri = "https://pulsoid.net/oauth2/device_authorization";
			using HttpClient httpClient = new();
			FormUrlEncodedContent formData = new(
			[
				new KeyValuePair<string, string>("client_id", Encoding.UTF8.GetString(Convert.FromBase64String(PublicClientID))),
				new KeyValuePair<string, string>("scope", "data:heart_rate:read")
			]);

			try
			{
				HttpResponseMessage httpResponse = await httpClient.PostAsync(initiateUri, formData);
				httpResponse.EnsureSuccessStatusCode();
				string responseBody = await httpResponse.Content.ReadAsStringAsync();
				return JsonSerializer.Deserialize<Json.DeviceAuthorizationFlow.InitialResponse>(responseBody);
			}
			catch
			{
				return null;
			}
		}

		public static async Task ValidateToken()
		{
			if (!MyRegex.GUID().IsMatch(ConfigData.PulsoidToken))
			{
				TokenValidity = TokenValidityStatus.Invalid;
				return;
			}

			using HttpClient httpClient = new();
			httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ConfigData.PulsoidToken);
			httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

			try
			{
				HttpResponseMessage httpResponse = await httpClient.GetAsync("https://dev.pulsoid.net/api/v1/token/validate");
				httpResponse.EnsureSuccessStatusCode();
				string resultBody = await httpResponse.Content.ReadAsStringAsync();

				Json.ValidateTokenResponse? resultJson = JsonSerializer.Deserialize<Json.ValidateTokenResponse>(resultBody);

				if (resultJson == null) return;
				string client_id = resultJson.ClientId ?? string.Empty;
				int expires_in = resultJson.ExpiresIn ?? 0;
				string profile_id = resultJson.ProfileId ?? string.Empty;
				List<string> scopes = resultJson.Scopes ?? [];

				if (scopes.Contains("data:heart_rate:read") && expires_in > 0) TokenValidity = TokenValidityStatus.Valid;
				else TokenValidity = TokenValidityStatus.Invalid;
			}
			catch (Exception ex)
			{
				if (ex.Message.Contains(" 401 ")) TokenValidity = TokenValidityStatus.Invalid;
				else TokenValidity = TokenValidityStatus.Unknown;
			}
		}

		private static string GetManualAuthorizationUri()
		{
			string baseUri = "https://pulsoid.net/oauth2/authorize";
			string client_id = Encoding.UTF8.GetString(Convert.FromBase64String(PublicClientID));
			string redirect_uri = "http://localhost:54269/pulsoidtokenredirect/";
			string response_type = "token";
			string scope = "data:heart_rate:read";
			string state = Guid.NewGuid().ToString();
			string response_mode = "web_page";

			string completeGetRequest =
				baseUri
				+ "?client_id=" + client_id
				+ "&redirect_uri=" +  redirect_uri
				+ "&response_type=" + response_type
				+ "&scope=" + scope
				+ "&state=" + state
				+ "&response_mode=" + response_mode
			;

			return completeGetRequest;
		}
	}
}