using SharpOSC;

namespace PulsoidToOSC
{
	internal class OSCParameter
	{
		public enum Types { Integer, Float, Float01, BoolToggle, BoolActive, Trend, Trend01 }
		public Types Type { get; set; } = Types.Integer;
		public string Name { get; set; } = string.Empty;

		public OscMessage? GetOscMessage(string oscPath)
		{
			if (Name == string.Empty || oscPath == string.Empty) return null;

			return Type switch
			{
				Types.Integer => new(oscPath + Name, HeartRate.HRValue),
				Types.Float => new(oscPath + Name, Math.Clamp(HeartRate.Remap(HeartRate.HRValue, ConfigData.HrFloatMin, ConfigData.HrFloatMax, -1f, 1f), -1f, 1f)),
				Types.Float01 => new(oscPath + Name, Math.Clamp(HeartRate.Remap(HeartRate.HRValue, ConfigData.HrFloatMin, ConfigData.HrFloatMax, 0f, 1f), 0f, 1f)),
				Types.BoolToggle => new(oscPath + Name, HeartRate.HBToggle),
				Types.BoolActive => new(oscPath + Name, HeartRate.HRValue > 0),
				Types.Trend => new(oscPath + Name, HeartRate.TrendF),
				Types.Trend01 => new(oscPath + Name, (HeartRate.TrendF + 1f) / 2f),
				_ => null
			};
		}
	}
}