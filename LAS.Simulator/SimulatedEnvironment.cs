using System;
using NLog;

namespace LAS.Simulator
{
	public static class SimulatedEnvironment
	{
		static readonly Logger logger = LogManager.GetCurrentClassLogger();
		static Random r = new Random();
		static DateTime startTime = DateTime.Now;

		public static bool SendPosition(SimulatedAmbulance a)
		{
			var coin = r.NextDouble();
			if (coin > .65) {
				return false;
			}

			return true;
		}

		public static bool RefuseMobilization(SimulatedAmbulance a)
		{
			var coin = r.NextDouble();
			if (coin >= .75) {
				return true;
			}

			return false;
		}

		public static bool PressOnSceneButton(SimulatedAmbulance a)
		{
			var coin = r.NextDouble();
			// Scenario 2
			if (DateTime.Now - startTime > TimeSpan.FromSeconds(15 * 60)) {
				if (coin > .7) {
					return false;
				}
			} else {
				if (coin > .9) {
					return false;
				}
			}

			return true;
		}

		public static bool PressAtHospitalButton(SimulatedAmbulance a)
		{
			var coin = r.NextDouble();
			// Scenario 2
			if (DateTime.Now - startTime > TimeSpan.FromSeconds(15 * 60)) {
				if (coin > .7) {
					return false;
				}
			} else {
				if (coin > .99) {
					return false;
				}
			}

			return true;
		}

		public static bool PressAtStationButton(SimulatedAmbulance a)
		{
			var coin = r.NextDouble();

			// Scenario 2
			if (DateTime.Now - startTime > TimeSpan.FromSeconds(15 * 60)) {
				if (coin > .2) {
					return false;
				}
			} else {
				if (coin > .5) {
					return false;
				}
			}

			return true;
		}

		public static bool InTrafficJam(SimulatedAmbulance a) {
			var coin = r.NextDouble();

			// Scenario 1
			//if (DateTime.Now - startTime > TimeSpan.FromSeconds(15 * 60)) {
			//	if (coin > .7) {
			//		return false;
			//	}
			//} else {
				if (coin > .01) {
					return false;
				}
			//}

			return true;
		}
	}
}
