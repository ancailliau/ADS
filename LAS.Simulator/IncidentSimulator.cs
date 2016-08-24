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
				Thread.Sleep(TimeSpan.FromSeconds(6));
			}
		}
	}
}
