﻿using SharpOSC;
using System.Diagnostics;
using Makaretu.Dns;
using System.Text.RegularExpressions;
using System.Net.NetworkInformation;
using System.Net;

namespace PulsoidToOSC
{
	internal static class VRCOSC
	{
		private const string _oscPath = "/avatar/parameters/";
		private static readonly Dictionary<string, VRCClient> VRCClients = [];

		public static string OSCPath
		{
			get => _oscPath;
		}

		private static DateTime lastVRCChatboxMessageTime = DateTime.MinValue;

		public static void SendHeartRates(int heartRate = 0)
		{
			if (ConfigData.VRCUseAutoConfig && VRCClients.Count > 0)
			{
				List<OscMessage> oscMessages = [
					new(OSCPath + "Heartrate", (heartRate / 127f) - 1f),		//Float ([0, 255] -> [-1, 1])
					new(OSCPath + "HeartRateFloat", (heartRate / 127f) - 1f),	//Float ([0, 255] -> [-1, 1])
					new(OSCPath + "Heartrate2", heartRate / 255f),				//Float ([0, 255] -> [0, 1]) 
					new(OSCPath + "HeartRateFloat01", heartRate / 255f),		//Float ([0, 255] -> [0, 1]) 
					new(OSCPath + "Heartrate3", heartRate),						//Int [0, 255]
					new(OSCPath + "HeartRateInt", heartRate),					//Int [0, 255]
					new(OSCPath + "HeartBeatToggle", MainProgram.HBToggle)		//Bool reverses with each update
				];

				foreach (VRCClient vrcClient in VRCClients.Values)
				{
					if (!(vrcClient.IsLocalHost || ConfigData.VRCSendToAllClinetsOnLAN) || vrcClient.OscSender == null) continue;

					if (ConfigData.VRCSendBPMToChatbox && heartRate > 0) SendVRCChatBox(vrcClient.OscSender, heartRate);
					else ClearVRCChatbox(vrcClient.OscSender);

					if (vrcClient.IsOscSenderSameAsGlobal && OSCPath == ConfigData.OSCPath) continue; // check against sending same values twice to one endpoint

					foreach (OscMessage message in oscMessages) if (message != null) vrcClient.OscSender.Send(message);
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
			if (lastVRCChatboxMessageTime.AddSeconds(2) < DateTime.UtcNow)
			{
				string message = ConfigData.VRCChatboxMessage.Contains("<bpm>") ? ConfigData.VRCChatboxMessage.Replace("<bpm>", heartRate.ToString()) : ConfigData.VRCChatboxMessage + heartRate ;
				message = ConvertSpecialCharacters(message);
				oscSender.Send(new OscMessage("/chatbox/input", message, true, false));
				lastVRCChatboxMessageTime = DateTime.UtcNow;
			}
		}

		public static void ClearVRCChatbox(UDPSender oscSender, bool tryAgainLater = true) // Not reliable due to VRC message rate limit - will retry after 2.5 sec cooldown
		{
			if (lastVRCChatboxMessageTime.Equals(DateTime.MinValue)) return;

			DateTime nextVRCChatboxMessageTime = lastVRCChatboxMessageTime.AddSeconds(2);

			if (nextVRCChatboxMessageTime < DateTime.UtcNow)
			{
				oscSender.Send(new OscMessage("/chatbox/input", "", true, false));
				lastVRCChatboxMessageTime = DateTime.MinValue;
			}
			else if (tryAgainLater && nextVRCChatboxMessageTime > DateTime.UtcNow)
			{
				TimeSpan delay = nextVRCChatboxMessageTime - DateTime.UtcNow;

				Task.Run(async () =>
				{
					await Task.Delay(delay + TimeSpan.FromMilliseconds(500));
					MainProgram.disp.Invoke(() => ClearVRCChatbox(oscSender, false));
				});
			}
		}

		private static string ConvertSpecialCharacters(string input)
		{
			const int required_width = 64;

			input = input.Replace("\\v", "\v");
			input = input.Replace("/v", "\v");
			input = input.Replace("\\n", "/n");

			return Regex.Replace(input, "/n", match =>
			{
				var spaces = match.Index == 0 ? 0 : (match.Index - input.LastIndexOf("/n", match.Index, StringComparison.Ordinal)) % required_width;
				var spaceCount = required_width - spaces;
				return spaces < 0 || spaceCount < 0 ? string.Empty : new string(' ', spaceCount);
			});
		}

		class VRCClient
		{
			public UDPSender? OscSender { get; private set; }
			public IPAddress OscUDPIP = IPAddress.None;
			public int OscUDPPort = 0;
			public bool IsLocalHost = false;
			public bool IsOscSenderSameAsGlobal = false;

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
			private static ServiceDiscovery? serviceDiscovery;
			private static MulticastService? multicastService;

			public static void SetupQuerry()
			{
				serviceDiscovery = new ServiceDiscovery();
				multicastService = new MulticastService();

				serviceDiscovery.ServiceInstanceDiscovered += (s, e) => OnServiceInstanceDiscovered(e);
				multicastService.NetworkInterfaceDiscovered += (s, e) => OnNetworkInterfaceDiscovered(e);
				multicastService.AnswerReceived += (s, e) => OnAnswerReceived(e);

				multicastService.Start();
			}

			private static void OnNetworkInterfaceDiscovered(NetworkInterfaceEventArgs e)
			{
				if (serviceDiscovery == null) return;

				foreach (var nic in e.NetworkInterfaces)
				{
					Debug.WriteLine($"NIC '{nic.Name}'");
				}

				// Ask for OSC service instances
				serviceDiscovery.QueryServiceInstances("_osc._udp"); // _oscjson._tcp.
			}

			private static void OnServiceInstanceDiscovered(ServiceInstanceDiscoveryEventArgs e)
			{
				if (multicastService == null) return;

				string serviceInstanceName = e.ServiceInstanceName.ToString();

				Match match = MyRegex.RegexVRC_ID_UDP().Match(serviceInstanceName);
				string id = match.Groups[1].Value;

				if (match.Success && id != string.Empty)
				{
					if (VRCClients.TryAdd(id, new()))
					{
						Debug.WriteLine($"added client with id: {id}");
					}
					if (VRCClients[id].OscUDPPort == 0 || VRCClients[id].OscUDPIP == IPAddress.None)
					{
						// Ask for service instance details 
						multicastService.SendQuery(e.ServiceInstanceName, type: DnsType.SRV);
					}
				}
			}

			private static void OnAnswerReceived(MessageEventArgs e)
			{
				if (multicastService == null) return;

				// Is this an answer to a service instance details?
				var servers = e.Message.Answers.OfType<SRVRecord>();
				foreach (var server in servers)
				{
					Match match = MyRegex.RegexVRC_ID().Match(server.Target.ToString());
					string id = match.Groups[1].Value;

					if (match.Success && id != string.Empty)
					{
						if (VRCClients.TryGetValue(id, out _))
						{
							VRCClients[id].OscUDPPort = server.Port;
							Debug.WriteLine($"added port {server.Port} to client with id: {id}");
						}
						if (VRCClients[id].OscUDPIP == IPAddress.None)
						{
							// Ask for the host IP addresses.
							multicastService.SendQuery(server.Target, type: DnsType.A);
							multicastService.SendQuery(server.Target, type: DnsType.AAAA);
						}
					}
				}

				// Is this an answer to host addresses?
				var addresses = e.Message.Answers.OfType<AddressRecord>();
				foreach (var address in addresses)
				{
					string addressName = address.Name.ToString();

					Match match = MyRegex.RegexVRC_ID().Match(addressName);
					string id = match.Groups[1].Value;

					if (match.Success && id != string.Empty)
					{
						if (!VRCClients.TryGetValue(id, out _)) return;

						bool isLocalIp = IsLocalhost(address.Address);

						VRCClients[id].OscUDPIP = isLocalIp ? IPAddress.Loopback : address.Address;
						VRCClients[id].IsLocalHost = isLocalIp;

						Debug.WriteLine($"added {(isLocalIp ? $"localhost ip {IPAddress.Loopback}" : $"ip {address.Address}")} to client with id: {id}");

						VRCClients[id].SetupOSCSender();
					}
				}
			}

			public static void StopQuerry()
			{
				VRCClients.Clear();
				serviceDiscovery?.Dispose();
				multicastService?.Stop();

				serviceDiscovery = null;
				multicastService = null;
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