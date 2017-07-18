using System;
using UCLouvain.AmbulanceSystem.MDTClient;
using UCLouvain.AmbulanceSystem.Core.Utils;
using Itinero;
using Itinero.LocalGeo;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using NLog;
using UCLouvain.AmbulanceSystem.Core.Messages;

namespace UCLouvain.AmbulanceSystem.Simulator
{
	public class SimulatedStation
	{
        public string StationId
        {
            get;
            set;
        }
        
        public string Name
        {
            get;
            set;
        }
        
        public string PrinterId
        {
            get;
            set;
        }
        
        public Coordinate Coordinates
        {
            get;
            set;
        }

        public List<SimulatedAmbulance> AmbulanceAtStation
        {
            get;
            set;
        }

		static readonly Logger logger = LogManager.GetCurrentClassLogger();
		static readonly Random r = new Random();

		public SimulatedStation(string identifier, string name, Coordinate coordinates, string printerId)
		{
            StationId = identifier;
            Name = name;
            PrinterId = printerId;
            Coordinates = coordinates;

            AmbulanceAtStation = new List<SimulatedAmbulance>();
            
            Register();
		}
        
        void Register ()
        {
            var sender = new RabbitMQMessageSender();
            var message = new RegisterAmbulanceStation(StationId, Name, PrinterId, Coordinates.Latitude, Coordinates.Longitude);
            sender.Send(message, "internal_comm_queue");
        }
	}
}
