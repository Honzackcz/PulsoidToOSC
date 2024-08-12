using SharpOSC;

namespace PulsoidToOSC
{
	internal static class HeartRate
	{
		public static bool HBToggle { get; private set; } = false;
		public enum Trends { None, Stable, Upward, Downward, StrongUpward, StrongDownward };
		public static Trends Trend { get; private set; } = Trends.None;

		private static readonly List<int> _recentHeartRates = [];

		public static void Send(int heartRate = 0)
		{
			if (heartRate < 0) return;

			HBToggle = !HBToggle;

			_recentHeartRates.Add(heartRate);
			if (_recentHeartRates.Count > 5)
			{
				_recentHeartRates.RemoveAt(0);
				Trend = CalcualteTrend(_recentHeartRates) switch
				{
					> 1f => Trends.StrongUpward,
					< -1f => Trends.StrongDownward,
					> 0.5f => Trends.Upward,
					< -0.5f => Trends.Downward,
					_ => Trends.Stable
				};
			}
			else
			{
				Trend = Trends.None;
			}

			VRCOSC.SendHeartRates(heartRate);

			if (MainProgram.OSCSender == null || !ConfigData.OSCUseManualConfig) return;

			foreach (OSCParameter oscParameter in ConfigData.OSCParameters)
			{
				OscMessage? oscMessage = oscParameter.GetOscMessage(ConfigData.OSCPath, heartRate, HBToggle);
				if (oscMessage != null) MainProgram.OSCSender.Send(oscMessage);
			}
		}

		public static void ResetTrends()
		{
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

			return (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
		}
	}
}
