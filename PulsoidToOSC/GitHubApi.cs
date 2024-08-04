using System.Net.Http;
using System.Text.Json;

namespace PulsoidToOSC
{
	internal static class GitHubApi
	{
		public enum VersionStatus { None, NewIsAvailable, UpToDate}

		public static async Task<VersionStatus> IsNewVersionAvailable(string owner, string repo, string versionToCheck)
		{
			try
			{
				using HttpClient client = new();
				client.DefaultRequestHeaders.UserAgent.TryParseAdd("request"); // Set the User-Agent header

				HttpResponseMessage response = await client.GetAsync($"https://api.github.com/repos/{owner}/{repo}/releases/latest");
				response.EnsureSuccessStatusCode();

				string content = await response.Content.ReadAsStringAsync();

				using JsonDocument document = JsonDocument.Parse(content);

				string latestVersion = document.RootElement.GetProperty("tag_name").GetString() ?? "v0.0.0";

				if (string.Compare(latestVersion, versionToCheck, StringComparison.Ordinal) > 0)
				{
					return VersionStatus.NewIsAvailable;
				}
				else
				{
					return VersionStatus.UpToDate;
				}
			}
			catch (Exception)
			{
				
			}

			return VersionStatus.None;
		}
	}
}