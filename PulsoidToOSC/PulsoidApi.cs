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
				public long MeasuredAt { get; set; } = 0L;

				[JsonPropertyName("data")]
				public WSMessage_Data Data { get; set; } = new();
			}
			public class WSMessage_Data
			{
				[JsonPropertyName("heart_rate")]
				public int HeartRate { get; set; } = 0;
			}

			public class ValidateTokenResponse
			{
				[JsonPropertyName("client_id")]
				public string ClientId { get; set; } = string.Empty;

				[JsonPropertyName("expires_in")]
				public int ExpiresIn { get; set; } = 0;

				[JsonPropertyName("profile_id")]
				public string ProfileId { get; set; } = string.Empty;

				[JsonPropertyName("scopes")]
				public List<string> Scopes { get; set; } = [];
			}

			public class DeviceAuthorizationFlow
			{
				public class InitialResponse
				{
					[JsonPropertyName("device_code")]
					public string DeviceCode { get; set; } = string.Empty;

					[JsonPropertyName("user_code")]
					public string UserCode { get; set; } = string.Empty;

					[JsonPropertyName("verification_uri")]
					public string VerificationUri { get; set; } = string.Empty;

					[JsonPropertyName("verification_uri_complete")]
					public string VerificationUriComplete { get; set; } = string.Empty;

					[JsonPropertyName("expires_in")]
					public int ExpiresIn { get; set; } = 0;

					[JsonPropertyName("interval")]
					public int Interval { get; set; } = 3; // Default to 3 seconds based on Pulsoid's documentation
				}
				public class TokenOKResponse
				{
					[JsonPropertyName("access_token")]
					public string AccessToken { get; set; } = string.Empty;

					[JsonPropertyName("expires_in")]
					public int ExpiresIn { get; set; } = 0;

					[JsonPropertyName("token_type")]
					public string TokenType { get; set; } = string.Empty;
				}
				public class TokenErrorResponse
				{
					[JsonPropertyName("error")]
					public string Error { get; set; } = string.Empty;

					[JsonPropertyName("error_description")]
					public string ErrorDescription { get; set; } = string.Empty;
				}
			}
		}

		public static bool ProcessWSMessage(string message, out long measuredAt, out int heartRate)
		{
			measuredAt = 0L;
			heartRate = 0;

			try
			{
				Json.WSMessage messageJson = JsonSerializer.Deserialize<Json.WSMessage>(message) ?? throw new JsonException("Failed to deserialize WS message");
				measuredAt = messageJson.MeasuredAt;
				heartRate = messageJson.Data.HeartRate;
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
		private static CancellationTokenSource GetPulsoidToken_Cts = new();
		private static bool GetPulsoidToken_IsRunning = false;

		public static void SetPulsoidToken(string? token, bool saveConfig = true)
		{
			if (token != null && token != ConfigData.PulsoidToken && MyRegex.GUID().IsMatch(token))
			{
				ConfigData.PulsoidToken = token;
				TokenValidity = TokenValidityStatus.Unknown;
				if (saveConfig) ConfigData.SaveConfig();
			}
		}

		public static async Task CancelGetPulsoidToken_DeviceAuthorizationFlow()
		{
			await GetPulsoidToken_Cts.CancelAsync();
			GetPulsoidToken_Cts = new();
		}

		public static async Task GetPulsoidToken_DeviceAuthorizationFlow()
		{
			if (GetPulsoidToken_IsRunning) await CancelGetPulsoidToken_DeviceAuthorizationFlow();
			GetPulsoidToken_IsRunning = true;

			Json.DeviceAuthorizationFlow.InitialResponse? initialResponse = null;
			try
			{
				initialResponse = await InitiateDeviceAuthorizationSession(GetPulsoidToken_Cts.Token);
			}
			catch (TaskCanceledException ex)
			{
				if (ex.CancellationToken.IsCancellationRequested)
				{
					GetPulsoidToken_IsRunning = false;
					return;
				}
			}

			if (initialResponse == null || string.IsNullOrEmpty(initialResponse.VerificationUriComplete))
			{
				Process.Start(new ProcessStartInfo
				{
					FileName = GetManualAuthorizationUri(),
					UseShellExecute = true
				});
				return;
			}

			Process.Start(new ProcessStartInfo
			{
				FileName = initialResponse.VerificationUriComplete,
				UseShellExecute = true
			});

			TokenValidity = TokenValidityStatus.Unknown;
			MainProgram.MainViewModel.OptionsViewModel.GeneralViewModel.TokenValidity = TokenValidity;

			DateTime expireTime = DateTime.UtcNow.AddSeconds(initialResponse.ExpiresIn);
			const string pollingUri = "https://pulsoid.net/oauth2/token";
			using HttpClient httpClient = new();
			FormUrlEncodedContent formData = new(
			[
				new KeyValuePair<string, string>("grant_type", "urn:ietf:params:oauth:grant-type:device_code"),
				new KeyValuePair<string, string>("device_code", initialResponse.DeviceCode),
				new KeyValuePair<string, string>("client_id", Encoding.UTF8.GetString(Convert.FromBase64String(PublicClientID)))
			]);

			while (true)
			{
				try
				{
					await Task.Delay(1000 * (initialResponse.Interval), GetPulsoidToken_Cts.Token);
				}
				catch
				{
					break;
				}

				if (DateTime.UtcNow > expireTime) break;

				HttpResponseMessage? httpResponse;
				
				try
				{
					httpResponse = await httpClient.PostAsync(pollingUri, formData, GetPulsoidToken_Cts.Token);
				}
				catch (TaskCanceledException ex)
				{
					if (ex.CancellationToken.IsCancellationRequested) break;
					continue;
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
						Json.DeviceAuthorizationFlow.TokenOKResponse tokenOKResponse = JsonSerializer.Deserialize<Json.DeviceAuthorizationFlow.TokenOKResponse>(httpResponseBody) ?? throw new JsonException("Failed to deserialize token OK response");

						if (MyRegex.GUID().IsMatch(tokenOKResponse.AccessToken) && tokenOKResponse.ExpiresIn > 0)
						{
							SetPulsoidToken(tokenOKResponse.AccessToken);
							MainProgram.MainViewModel.OptionsViewModel.GeneralViewModel.TokenText = ConfigData.PulsoidToken;
							MainProgram.MainViewModel.OptionsViewModel.GeneralViewModel.TokenValidity = TokenValidity;
						}

						break;
					}
					catch
					{
						break;
					}
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

			GetPulsoidToken_IsRunning = false;
			await ValidateToken();
			MainProgram.MainViewModel.OptionsViewModel.GeneralViewModel.TokenValidity = TokenValidity;
		}

		private static async Task<Json.DeviceAuthorizationFlow.InitialResponse?> InitiateDeviceAuthorizationSession(CancellationToken cancellationToken)
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
				HttpResponseMessage httpResponse = await httpClient.PostAsync(initiateUri, formData, cancellationToken);
				httpResponse.EnsureSuccessStatusCode();
				string responseBody = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
				return JsonSerializer.Deserialize<Json.DeviceAuthorizationFlow.InitialResponse>(responseBody);
			}
			catch (TaskCanceledException ex)
			{
				if (ex.CancellationToken.IsCancellationRequested) throw;
			}

			return null;
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
				string client_id = resultJson.ClientId;
				int expires_in = resultJson.ExpiresIn;
				string profile_id = resultJson.ProfileId;
				List<string> scopes = resultJson.Scopes;

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