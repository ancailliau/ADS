using System;
using PetaPoco;
using LAS.Core.Repository;
using System.Threading;
using NLog;
using LAS.Core.Utils;
using LAS.Core.Messages;

namespace LAS.Simulator
{
	public class IncidentSimulator
	{
		static readonly Logger logger = LogManager.GetCurrentClassLogger();
		IncidentRepository incRepository;

		float maxlat = 50.875707f;
		float minlon = 4.327109f;
		float minlat = 50.796478f; 
		float maxlon = 4.455275f;

		static Random random = new Random();
		MessageSender sender = new MessageSender ();

		public IncidentSimulator(Database db)
		{
			this.incRepository = new IncidentRepository(db);
		}

		public void Start()
		{
			logger.Info("Start simulating incidents");
			new Thread(Loop).Start();
		}

		static DateTime startTime = DateTime.Now;

		void Loop()
		{
			while (true) {
				int openIncidents = incRepository.CountOpenIncidents();
				if (openIncidents < 10) {
					// start a new incident at a random location
					var incidentLat = (float)(minlat + random.NextDouble() * (maxlat - minlat));
					var incidentLong = (float)(minlon + random.NextDouble() * (maxlon - minlon));

					var m = new IncidentForm(incidentLat, incidentLong);
					sender.Send(m);
				} else {
					logger.Info("10 incidents are yet open");
				}
				// Scenario 2
				//if (DateTime.Now - startTime > TimeSpan.FromMinutes(12)) {
				//	if (DateTime.Now - startTime < TimeSpan.FromMinutes(30)) {
				//		var time = 20 - ((DateTime.Now - startTime - TimeSpan.FromMinutes(12)).Minutes);
				//		logger.Info("Sleep " + Math.Max(5, time) + " seconds");
				//		Thread.Sleep(TimeSpan.FromSeconds(Math.Max(5, time)));
				//	} else {
				//		var time = 5 + ((DateTime.Now - startTime - TimeSpan.FromMinutes(30)).Minutes);
				//		logger.Info("Sleep "+Math.Min(20, time)+" seconds");
				//		Thread.Sleep(TimeSpan.FromSeconds(Math.Min(20, time)));
				//	}
				//} else {
				//logger.Info("Sleep 20 seconds");
					Thread.Sleep(TimeSpan.FromSeconds(20));
				//}
			}
		}
	}
}
