using System;
using LAS.Core.Domain;
using LAS.Core.Messages;
using LAS.Core.Utils;
using NLog;
using LAS.Core.Repository;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using PetaPoco;
using LAS.Server.Allocators;

namespace LAS.Server
{
	public class Orchestrator
	{
		static readonly Logger logger = LogManager.GetCurrentClassLogger();

		IncidentProcessor ip;
		//AmbulanceAllocator allocator;
		//AmbulanceMobilizator mobilizator;
		//TrafficJamReallocator trafficJamReallocator;
		//Cancelator cancelator;

		internal readonly AmbulanceRepository ambulanceRepository;
		internal readonly HospitalRepository hospitalRepository;
		internal readonly IncidentRepository incidentRepository;
		internal readonly AllocationRepository allocationRepository;

		internal MapService mapService;

		Checker checker;

		public Orchestrator(IDatabaseBuildConfiguration config)
		{
			var db = new Database(config);
			ambulanceRepository = new AmbulanceRepository(db);
			hospitalRepository = new HospitalRepository(db);
			incidentRepository = new IncidentRepository(db);
			allocationRepository = new AllocationRepository(db);

			mapService = new MapService();

			ip = new IncidentProcessor(this);
			//allocator = new AmbulanceAllocator(mapService, new LoggedDatabase(config));
			//mobilizator = new AmbulanceMobilizator(new Database (config));
			//trafficJamReallocator = new TrafficJamReallocator(mapService, new LoggedDatabase(config));
			//cancelator = new Cancelator(new Database(config));

			checker = new Checker(config);
			checker.Start();

		}

		internal void Start()
		{
            //var listener = new TCPListeningServer(this);
            var listener = new RabbitMQListeningServer("incident_queue", new MessageProcessor(this));
			var ts = new ThreadStart(listener.Run);
			var t = new Thread(ts);
			t.Start();
            
            var listener_internal_comm = new RabbitMQListeningServer("internal_comm_queue", new MessageProcessor(this));
            var ts2 = new ThreadStart(listener_internal_comm.Run);
            var t2 = new Thread(ts2);
            t2.Start();
		}

		public void Process(Message m)
		{
			try {
				if (m is IncidentForm) {
					NewIncident((IncidentForm)m);

				} else if (m is RegisterAmbulanceMessage) {
					RegisterNewAmbulance((RegisterAmbulanceMessage)m);

				} else if (m is PositionMessage) {
					UpdatePosition((PositionMessage)m);

				} else if (m is AmbulanceStatusMessage) {
					UpdateStatus((AmbulanceStatusMessage)m);

				} else if (m is MobilizationOrderRefusal) {
					var refusal = (MobilizationOrderRefusal)m;
					allocationRepository.RefuseMobilization(refusal.AllocationId);

				} else if (m is DestinationUnreachableMessage) {
					allocationRepository.MarkUnreachable(((DestinationUnreachableMessage)m).AllocationId);

				} else if (m is InTrafficJamMessage) {
					ambulanceRepository.MarkInTrafficJam(((InTrafficJamMessage)m).AmbulanceId, true);

				} else if (m is NotInTrafficJamMessage) {
					ambulanceRepository.MarkInTrafficJam(((NotInTrafficJamMessage)m).AmbulanceId, false);

				} else if (m is DeployAllocatorMessage) {
					logger.Info("Message DeployAllocatorMessage Received");
					var ma = (DeployAllocatorMessage)m;
					checker.ReplaceAllocator(ma.Allocator);

				} else if (m is MobilizationConfirmation mobConfirmation) {
                    allocationRepository.SetMobilizationReceived(mobConfirmation.AllocationId);

                } else if (m is CancellationConfirmation cancelConfirmation) {
                    allocationRepository.ConfirmCancel(cancelConfirmation.AllocationId);

                } else {
					throw new NotImplementedException();
				}
			} catch (Exception e) {
				logger.Error("Error in processing message: " + m);
				logger.Error(e.Message);
				logger.Error(e.StackTrace);
			}
		}

		public void RegisterNewAmbulance(RegisterAmbulanceMessage m)
		{
			Ambulance a;
			if (ambulanceRepository.Contains(m.Identifier)) {
				logger.Info("Updating already registered ambulance");
				a = ambulanceRepository.Get(m.Identifier);
				a.SetStatus(AmbulanceStatus.Unavailable);
				a.SetPort(m.ListeningQueue);
				a.SetPosition(m.Latitude, m.Longitude);
				ambulanceRepository.Update(a);

				allocationRepository.CancelAllOpenAllocation(a.AmbulanceId, true);

			} else {
				logger.Info("Registering new ambulance");
				a = ambulanceRepository.AddAmbulance(m.Identifier,
				                                     m.Latitude,
				                                     m.Longitude,
				                                     AmbulanceStatus.Unavailable,
				                                     m.ListeningQueue);
			}
		}

		public void UpdatePosition(PositionMessage m)
		{
			logger.Info("UpdatePosition: {0} - {1},{2}", m.AmbulanceIdentifier, m.Latitude, m.Longitude);
			ambulanceRepository.UpdatePosition (m.AmbulanceIdentifier, m.Latitude, m.Longitude);
		}

		public void UpdateStatus(AmbulanceStatusMessage m)
		{
			logger.Info("UpdateStatus: {0} - {1}", m.AmbulanceIdentifier, m.Status);
			ambulanceRepository.UpdateStatus(m.AmbulanceIdentifier, m.Status);

			if (m.Status == AmbulanceStatus.AvailableAtStation
				|| m.Status == AmbulanceStatus.AvailableRadio
			    || m.Status == AmbulanceStatus.Unavailable) {

				incidentRepository.CloseAllocatedIncidents(m.AmbulanceIdentifier);
			}

			if (m.Status == AmbulanceStatus.Leaving) {
				allocationRepository.SetMobilizationConfirmation(m.AmbulanceIdentifier);
			}
		}

		internal void NewIncident(IncidentForm i)
		{
			ip.Process(i);
		}

	}
}
