using SharpOSC;

namespace PulsoidToOSC
{
	internal class OSCParameter
	{
		public enum Types { Integer, Float, Float01, BoolToggle, TrendF, TrendF01 }
		public Types Type { get; set; } = Types.Integer;
		public string Name { get; set; } = string.Empty;

		public OscMessage? GetOscMessage(string oscPath, int heartRate, bool hbToggle, float trendF)
		{
			if (Name == string.Empty || oscPath == string.Empty) return null;

			return Type switch
			{
				Types.Integer => new(oscPath + Name, heartRate),
				Types.Float => new(oscPath + Name, (heartRate / 127f) - 1f),
				Types.Float01 => new(oscPath + Name, heartRate / 255f),
				Types.BoolToggle => new(oscPath + Name, hbToggle),
				Types.TrendF => new(oscPath + Name, trendF),
				Types.TrendF01 => new(oscPath + Name, (trendF + 1f) / 2f),
				_ => null
			};
		}
	}
}