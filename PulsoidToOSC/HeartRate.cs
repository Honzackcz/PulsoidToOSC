using SharpOSC;

namespace PulsoidToOSC
{
	internal static class HeartRate
	{
		public static int HRValue {  get; private set; } = 0;
		public static bool HBToggle { get; private set; } = false;
		public enum Trends { None, Stable, Upward, Downward, StrongUpward, StrongDownward };
		public static Trends Trend { get; private set; } = Trends.None;
		public static float TrendF { get; private set; } = 0;

		private static readonly List<int> _recentHeartRates = [];

		public static void Send(int heartRate = 0)
		{
			if (heartRate < 0)
			{
				return;
			}
			else if (heartRate == 0)
			{
				HRValue = 0;
				HBToggle = false;
				TrendF = 0;
				Trend = Trends.None;
			}
			else
			{
				HRValue = Math.Clamp(heartRate + ConfigData.HrOffset, 0, 255);
				HBToggle = !HBToggle;

				_recentHeartRates.Add(HRValue);
				if (_recentHeartRates.Count > 5)
				{
					_recentHeartRates.RemoveAt(0);
					TrendF = Math.Clamp(Remap(CalcualteTrend(_recentHeartRates), -ConfigData.HrTrendMin, ConfigData.HrTrendMax, -1f, 1f), -1f, 1f);
					Trend = TrendF switch
					{
						> 0.5f => Trends.StrongUpward,
						< -0.5f => Trends.StrongDownward,
						> 0.25f => Trends.Upward,
						< -0.25f => Trends.Downward,
						_ => Trends.Stable
					};
				}
				else
				{
					TrendF = 0;
					Trend = Trends.None;
				}
			}
			
			VRCOSC.SendHeartRates();

			if (MainProgram.OSCSender == null || !ConfigData.OSCUseManualConfig) return;

			foreach (OSCParameter oscParameter in ConfigData.OSCParameters)
			{
				OscMessage? oscMessage = oscParameter.GetOscMessage(ConfigData.OSCPath);
				if (oscMessage != null) MainProgram.OSCSender.Send(oscMessage);
			}
		}

		public static void Reset()
		{
			HRValue = 0;
			HBToggle = false;
			TrendF = 0;
			Trend = Trends.None;
			_recentHeartRates.Clear();
		}

		private static float CalcualteTrend(List<int> values)
		{
			int n = values.Count;
			int sumX = 0, sumY = 0, sumXY = 0, sumX2 = 0;

			for (int i = 0; i < n; i++)
			{
				sumX += i;
				sumY += values[i];
				sumXY += i * values[i];
				sumX2 += i * i;
			}

			return (float) (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
		}

		public static float Remap(float source, float sourceFrom, float sourceTo, float targetFrom, float targetTo)
		{
			return targetFrom + (source - sourceFrom) * (targetTo - targetFrom) / (sourceTo - sourceFrom);
		}
	}
}