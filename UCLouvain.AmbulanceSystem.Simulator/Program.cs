using System;
using UCLouvain.AmbulanceSystem.Core.Utils;
using NLog;
using Itinero.LocalGeo;
using System.Collections.Generic;
using PetaPoco;
using System.Configuration;
using System.Threading;

namespace UCLouvain.AmbulanceSystem.Simulator
{
	class MainClass
	{
		static readonly Logger logger = LogManager.GetCurrentClassLogger();



		public static void Main(string[] args)
		{
			logger.Info("Starting simulator");
			var connectionString = ConfigurationManager.ConnectionStrings["postgres"].ConnectionString;
			var provider = new PostgreSQLDatabaseProvider();

			var config = DatabaseConfiguration.Build()
										  .UsingConnectionString(connectionString)
										  .UsingProvider(provider)
										  .UsingDefaultMapper<ConventionMapper>();

			var incSimulator = new IncidentSimulator(new Database(config));

			var mapService = new MapService();
			var hospitals = new Dictionary<string, Coordinate>();
			hospitals.Add("HOP1", new Coordinate(50.834957f, 4.348430f));
			hospitals.Add("HOP2", new Coordinate(50.814250f, 4.357530f));
			hospitals.Add ("HOP3",  new Coordinate(50.853853f, 4.360663f));
			hospitals.Add ("HOP4",  new Coordinate(50.852575f, 4.452851f));
			hospitals.Add ("HOP5",  new Coordinate(50.845607f, 4.317075f));
			hospitals.Add ("HOP6",  new Coordinate(50.804648f, 4.367769f));
			hospitals.Add ("HOP7",  new Coordinate(50.825022f, 4.379507f));
			hospitals.Add ("HOP8",  new Coordinate(50.813997f, 4.259632f));
			//hospitals.Add ("HOP8",  new Coordinate(50.8128775f, 4.2649523f));
			hospitals.Add ("HOP9",  new Coordinate(50.905819f, 4.390159f));
			hospitals.Add ("HOP10", new Coordinate(50.8885403f, 4.3288842f));
			hospitals.Add ("HOP11", new Coordinate(50.833261f, 4.347095f));
			hospitals.Add ("HOP12", new Coordinate(50.8872628f, 4.3089256f));
			hospitals.Add ("HOP13", new Coordinate (50.842471f, 4.399073f));

			var pasiDelta       = new SimulatedStation("pasiDelta", "PASI Delta", new Coordinate(50.818754f, 4.402740f), "pasiDelta_printer");
			var hopIxelles      = new SimulatedStation("hopIxelles", "Hopital d'Ixelles", new Coordinate(50.824938f, 4.379172f), "hopIxelles_printer");
			var pasiAnderlecht  = new SimulatedStation("pasiAnderlecht", "PASI Anderlecht", new Coordinate(50.832740f, 4.311812f), "pasiAnderlecht_printer");
			var pasiEvere       = new SimulatedStation("pasiEvere", "PASI Evere", new Coordinate(50.870964f, 4.417631f), "pasiEvere_printer");
			var pasiVUB         = new SimulatedStation("pasiVUB", "PASI VUB", new Coordinate(50.890831f, 4.308449f), "pasiVUB_printer");
			var pasiChenaie     = new SimulatedStation("pasiChenaie", "PASI Chenaie", new Coordinate(50.783341f, 4.356218f), "pasiChenaie_printer");
			var pasiCite        = new SimulatedStation("pasiCite", "PASI Cité", new Coordinate(50.849709f, 4.361116f), "pasiCite_printer");
			var pasiUCL         = new SimulatedStation("pasiUCL", "PASI UCL", new Coordinate(50.851937f, 4.460279f), "pasiUCL_printer");
			var heliport        = new SimulatedStation("heliport", "Caserne de l'Héliport", new Coordinate(50.859485f, 4.351848f), "heliport_printer");
			var moliere         = new SimulatedStation("moliere", "Hopital Molière", new Coordinate(50.815206f, 4.342141f), "moliere_printer");
			var saintpierre     = new SimulatedStation("saintpierre", "Hopital Saint-Pierre", new Coordinate(50.835228f, 4.348342f), "saintpierre_printer");
            
			logger.Info("Adding ambulances");

			var a01 = new SimulatedAmbulance("A1", mapService, hospitals, pasiAnderlecht);
			var a02 = new SimulatedAmbulance("A2", mapService, hospitals, pasiDelta);
			var a03 = new SimulatedAmbulance("A3", mapService, hospitals, pasiVUB);
			var a04 = new SimulatedAmbulance("A4", mapService, hospitals, pasiCite);
			var a05 = new SimulatedAmbulance("A5", mapService, hospitals, pasiVUB);
			var a06 = new SimulatedAmbulance("A6", mapService, hospitals, heliport);
			var a07 = new SimulatedAmbulance("A7", mapService, hospitals, heliport);
			var a08 = new SimulatedAmbulance("A8", mapService, hospitals, heliport);
			var a09 = new SimulatedAmbulance("A9", mapService, hospitals, hopIxelles);
			var a10 = new SimulatedAmbulance("A10", mapService, hospitals, pasiUCL);
			var a11 = new SimulatedAmbulance("A11", mapService, hospitals, pasiUCL);
			var a12 = new SimulatedAmbulance("A12", mapService, hospitals, pasiCite);
			var a13 = new SimulatedAmbulance("A13", mapService, hospitals, moliere);
			var a14 = new SimulatedAmbulance("A14", mapService, hospitals, pasiVUB);
			var a15 = new SimulatedAmbulance("A15", mapService, hospitals, pasiDelta); 
			//var a16 = new SimulatedAmbulance("A16", mapService, hospitals, saintpierre);
			//var a17 = new SimulatedAmbulance("A17", mapService, hospitals, heliport);
			//var a18 = new SimulatedAmbulance("A18", mapService, hospitals, pasiChenaie);
			//var a19 = new SimulatedAmbulance("A19", mapService, hospitals, pasiChenaie);
			//var a20 = new SimulatedAmbulance("A20", mapService, hospitals, saintpierre);
			//var a21 = new SimulatedAmbulance("A21", mapService, hospitals, saintpierre);
			//var a22 = new SimulatedAmbulance("A22", mapService, hospitals, pasiEvere);
			//var a23 = new SimulatedAmbulance("A23", mapService, hospitals, pasiEvere);
			//var a24 = new SimulatedAmbulance("A24", mapService, hospitals, pasiEvere);
			//var a25 = new SimulatedAmbulance("A25", mapService, hospitals, pasiCite);
			//var a26 = new SimulatedAmbulance("A26", mapService, hospitals, pasiCite);

			logger.Info("Ambulances added");

			Thread.Sleep(TimeSpan.FromSeconds (5));
			incSimulator.Start();
		}


	}
}
