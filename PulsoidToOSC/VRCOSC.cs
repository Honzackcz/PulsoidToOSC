using SharpOSC;
using Makaretu.Dns;
using System.Text.RegularExpressions;
using System.Net.NetworkInformation;
using System.Net;

namespace PulsoidToOSC
{
	internal static class VRCOSC
	{
		public static string OSCPath { get; } = "/avatar/parameters/";
		private static readonly Dictionary<string, VRCClient> VRCClients = [];
		private static readonly Dictionary<HeartRate.Trends, string> HeartRateTrendStrings = new() 
		{
			{ HeartRate.Trends.None, "" },
			{ HeartRate.Trends.Stable, "▶" },
			{ HeartRate.Trends.Upward, "↗" },
			{ HeartRate.Trends.Downward, "↘" },
			{ HeartRate.Trends.StrongUpward, "⏫" },
			{ HeartRate.Trends.StrongDownward, "⏬" }
		};

		private static DateTime _lastVRCChatboxMessageTime = DateTime.MinValue;

		public static void SendHeartRates(int heartRate = 0)
		{
			if (ConfigData.VRCUseAutoConfig && VRCClients.Count > 0)
			{
				foreach (VRCClient vrcClient in VRCClients.Values)
				{
					if (!(vrcClient.IsLocalHost || ConfigData.VRCSendToAllClinetsOnLAN) || vrcClient.OscSender == null) continue;

					if (ConfigData.VRCSendBPMToChatbox && heartRate > 0) SendVRCChatBox(vrcClient.OscSender, heartRate);
					else ClearVRCChatbox(vrcClient.OscSender);

					if (vrcClient.IsOscSenderSameAsGlobal && OSCPath == ConfigData.OSCPath) continue; // check against sending same values twice to one endpoint

					foreach (OSCParameter oscParameter in ConfigData.OSCParameters)
					{
						OscMessage? oscMessage = oscParameter.GetOscMessage(OSCPath, heartRate, HeartRate.HBToggle);
						if (oscMessage != null) vrcClient.OscSender.Send(oscMessage);
					}
				}
			}
			else if (MainProgram.OSCSender != null && ConfigData.OSCUseManualConfig) // send chatbox message to manually set OSC endpoint when VRC auto config is disabled
			{
				if (ConfigData.VRCSendBPMToChatbox && heartRate > 0) SendVRCChatBox(MainProgram.OSCSender, heartRate);
				else ClearVRCChatbox(MainProgram.OSCSender);
			}
		}

		public static void SendVRCChatBox(UDPSender oscSender, int heartRate) // Not reliable due to VRC message rate limit - will not send in 2 sec cooldown
		{
			if (_lastVRCChatboxMessageTime.AddSeconds(1.9) < DateTime.UtcNow)
			{
				string message = ConfigData.VRCChatboxMessage.Contains("<bpm>") ? ConfigData.VRCChatboxMessage.Replace("<bpm>", heartRate.ToString()) : ConfigData.VRCChatboxMessage + heartRate;
				message = message.Replace("<trend>", HeartRateTrendStrings[HeartRate.Trend]);
				message = ConvertSpecialCharacters(message);
				oscSender.Send(new OscMessage("/chatbox/input", message, true, false));
				_lastVRCChatboxMessageTime = DateTime.UtcNow;
			}
		}

		public static void ClearVRCChatbox(UDPSender oscSender, bool tryAgainLater = true) // Not reliable due to VRC message rate limit - will retry after 2.5 sec cooldown
		{
			if (_lastVRCChatboxMessageTime.Equals(DateTime.MinValue)) return;

			DateTime nextVRCChatboxMessageTime = _lastVRCChatboxMessageTime.AddSeconds(2);

			if (nextVRCChatboxMessageTime < DateTime.UtcNow)
			{
				oscSender.Send(new OscMessage("/chatbox/input", "", true, false));
				_lastVRCChatboxMessageTime = DateTime.MinValue;
			}
			else if (tryAgainLater && nextVRCChatboxMessageTime > DateTime.UtcNow)
			{
				TimeSpan delay = nextVRCChatboxMessageTime - DateTime.UtcNow;

				Task.Run(async () =>
				{
					await Task.Delay(delay + TimeSpan.FromMilliseconds(500));
					MainProgram.Disp.Invoke(() => ClearVRCChatbox(oscSender, false));
				});
			}
		}

		private static string ConvertSpecialCharacters(string input)
		{
			const int required_width = 64;

			input = input.Replace("\\v", "\v");
			input = input.Replace("/v", "\v");
			input = input.Replace("\\n", "/n");

			return MyRegex.LineEnd().Replace(input, match =>
			{
				int spaces = match.Index == 0 ? 0 : (match.Index - input.LastIndexOf("/n", match.Index, StringComparison.Ordinal)) % required_width;
				int spaceCount = required_width - spaces;
				return spaces < 0 || spaceCount < 0 ? string.Empty : new string(' ', spaceCount);
			});
		}

		class VRCClient
		{
			public UDPSender? OscSender { get; private set; }
			public IPAddress OscUDPIP { get; set; } = IPAddress.None;
			public int OscUDPPort { get; set; } = 0;
			public bool IsLocalHost { get; set; } = false;
			public bool IsOscSenderSameAsGlobal { get; private set; } = false;

			public void SetupOSCSender()
			{
				HashSet<(IPAddress, int)> seen = [];

				foreach (KeyValuePair<string, VRCClient> kvp in VRCClients)
				{
					if (!seen.Add((kvp.Value.OscUDPIP, kvp.Value.OscUDPPort)))
					{
						VRCClients[kvp.Key].OscSender?.Close();
						VRCClients[kvp.Key].OscSender = null;
					}
					else if (this == kvp.Value)
					{
						OscSender = new(OscUDPIP.ToString(), OscUDPPort);
						if (ConfigData.OSCIP == OscUDPIP && ConfigData.OSCPort == OscUDPPort) IsOscSenderSameAsGlobal = true;
					}
				}
			}
		}

		internal static class Query
		{
			private static ServiceDiscovery? _serviceDiscovery;
			private static MulticastService? _multicastService;

			public static void SetupQuerry()
			{
				_serviceDiscovery = new ServiceDiscovery();
				_multicastService = new MulticastService();

				_serviceDiscovery.ServiceInstanceDiscovered += (s, e) => OnServiceInstanceDiscovered(e);
				_multicastService.NetworkInterfaceDiscovered += (s, e) => OnNetworkInterfaceDiscovered(e);
				_multicastService.AnswerReceived += (s, e) => OnAnswerReceived(e);

				_multicastService.Start();
			}

			private static void OnNetworkInterfaceDiscovered(NetworkInterfaceEventArgs e)
			{
				if (_serviceDiscovery == null) return;

				// Ask for OSC service instances
				_serviceDiscovery.QueryServiceInstances("_osc._udp"); // _oscjson._tcp.
			}

			private static void OnServiceInstanceDiscovered(ServiceInstanceDiscoveryEventArgs e)
			{
				if (_multicastService == null) return;

				string serviceInstanceName = e.ServiceInstanceName.ToString();

				Match match = MyRegex.VRC_ID_UDP().Match(serviceInstanceName);
				string id = match.Groups[1].Value;

				if (match.Success && id != string.Empty)
				{
					VRCClients.TryAdd(id, new());

					if (VRCClients[id].OscUDPPort == 0 || VRCClients[id].OscUDPIP == IPAddress.None)
					{
						// Ask for service instance details 
						_multicastService.SendQuery(e.ServiceInstanceName, type: DnsType.SRV);
					}
				}
			}

			private static void OnAnswerReceived(MessageEventArgs e)
			{
				if (_multicastService == null) return;

				// Is this an answer to a service instance details?
				IEnumerable<SRVRecord> servers = e.Message.Answers.OfType<SRVRecord>();
				foreach (var server in servers)
				{
					Match match = MyRegex.VRC_ID().Match(server.Target.ToString());
					string id = match.Groups[1].Value;

					if (match.Success && id != string.Empty)
					{
						if (VRCClients.TryGetValue(id, out _))
						{
							VRCClients[id].OscUDPPort = server.Port;
						}
						if (VRCClients[id].OscUDPIP == IPAddress.None)
						{
							// Ask for the host IP addresses.
							_multicastService.SendQuery(server.Target, type: DnsType.A);
							_multicastService.SendQuery(server.Target, type: DnsType.AAAA);
						}
					}
				}

				// Is this an answer to host addresses?
				IEnumerable<AddressRecord> addresses = e.Message.Answers.OfType<AddressRecord>();
				foreach (AddressRecord address in addresses)
				{
					string addressName = address.Name.ToString();

					Match match = MyRegex.VRC_ID().Match(addressName);
					string id = match.Groups[1].Value;

					if (match.Success && id != string.Empty)
					{
						if (!VRCClients.TryGetValue(id, out _)) return;

						bool isLocalIp = IsLocalhost(address.Address);

						VRCClients[id].OscUDPIP = isLocalIp ? IPAddress.Loopback : address.Address;
						VRCClients[id].IsLocalHost = isLocalIp;
						VRCClients[id].SetupOSCSender();
					}
				}
			}

			public static void StopQuerry()
			{
				VRCClients.Clear();
				_serviceDiscovery?.Dispose();
				_multicastService?.Dispose();

				_serviceDiscovery = null;
				_multicastService = null;
			}

			private static bool IsLocalhost(IPAddress ipAddress)
			{
				if (IPAddress.IsLoopback(ipAddress)) return true;

				foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
				{
					foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
					{
						if (ip.Address.Equals(ipAddress)) return true;
					}
				}

				return false;
			}
		}
	}
}