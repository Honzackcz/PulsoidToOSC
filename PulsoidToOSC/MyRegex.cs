using System.Text.RegularExpressions;

namespace PulsoidToOSC
{
	internal static partial class MyRegex
	{
		[GeneratedRegex(@"^((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$")]
		public static partial Regex IP();

		[GeneratedRegex(@"^[0-9a-fA-F]{8}-([0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}$")]
		public static partial Regex GUID();

		[GeneratedRegex(@"VRChat-Client-([A-Za-z0-9]+)\._osc\._udp\.")]
		public static partial Regex VRC_ID_UDP();

		[GeneratedRegex(@"VRChat-Client-([A-Za-z0-9]+)\._oscjson\._tcp\.")]
		public static partial Regex VRC_ID_TCP();

		[GeneratedRegex(@"VRChat-Client-([A-Za-z0-9]+)\.osc\.")]
		public static partial Regex VRC_ID();

		[GeneratedRegex(@"[A-Za-z0-9]")]
		public static partial Regex TokenSymbolToHide();

		[GeneratedRegex(@"[^A-Fa-f0-9-]")]
		public static partial Regex NotTokenSymbol();

		[GeneratedRegex(@"/n")]
		public static partial Regex LineEnd();

		[GeneratedRegex(@"^#([A-Fa-f0-9]{6})$")]
		public static partial Regex RGBHexCode();

		[GeneratedRegex(@"[^A-Fa-f0-9]")]
		public static partial Regex NotHexCodeSymbol();
	}
}