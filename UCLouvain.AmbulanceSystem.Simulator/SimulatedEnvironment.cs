using System;
using NLog;

namespace UCLouvain.AmbulanceSystem.Simulator
{
	public static class SimulatedEnvironment
	{
		static readonly Logger logger = LogManager.GetCurrentClassLogger();
		static Random r = new Random();
		static DateTime startTime = DateTime.Now;

        static TimeSpan start_traffic_jam = new TimeSpan(7, 0, 0);
        static TimeSpan end_traffic_jam = new TimeSpan(9, 0, 0);

        static TimeSpan start_forget_button = new TimeSpan(3, 0, 0);
        static TimeSpan end_forget_button = new TimeSpan(6, 0, 0);

        static TimeSpan start_avls_failure = new TimeSpan(14, 0, 0);
        static TimeSpan end_avls_failure = new TimeSpan(16, 0, 0);
        
		public static bool SendPosition(SimulatedAmbulance a)
		{
			var coin = r.NextDouble();

            // Scenario 3
            var now = DateTime.Now.TimeOfDay;
            if (now > start_avls_failure & now < end_avls_failure) {
                if (coin > .6) {
                    return false;
                }
            } else {
                if (coin > .2) {
                    return false;
                }
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
            var now = DateTime.Now.TimeOfDay;
            if (now > start_forget_button & now < end_forget_button) {
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
            var now = DateTime.Now.TimeOfDay;
            if (now > start_forget_button & now < end_forget_button) {
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
            var now = DateTime.Now.TimeOfDay;
            if (now > start_forget_button & now < end_forget_button) {
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
            var now = DateTime.Now.TimeOfDay;
            if (now > start_traffic_jam & now < end_traffic_jam) {
				if (coin > .7) {
					return false;
				}
			} else {
				if (coin > .01) {
					return false;
				}
			}

			return true;
		}
	}
}
