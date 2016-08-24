using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LAS.Core.Domain;
using LAS.Core.Repository;
using LAS.Core.Utils;
using LAS.Server.Allocators;
using NLog;
using PetaPoco;

namespace LAS.Server
{
	public class Checker
	{
		static readonly Logger logger = LogManager.GetCurrentClassLogger();

		AmbulanceAllocator allocator;
		AmbulanceMobilizator mobilizator;
		TrafficJamReallocator trafficJamReallocator;
		Cancelator cancelator;

		internal readonly AmbulanceRepository ambulanceRepository;
		internal readonly HospitalRepository hospitalRepository;
		internal readonly IncidentRepository incidentRepository;
		internal readonly AllocationRepository allocationRepository;

		internal MapService mapService;

		public Checker(IDatabaseBuildConfiguration config)
		{
			var db = new Database(config);
			ambulanceRepository = new AmbulanceRepository(db);
			hospitalRepository = new HospitalRepository(db);
			incidentRepository = new IncidentRepository(db);
			allocationRepository = new AllocationRepository(db);

			mapService = new MapService();

			allocator = new AmbulanceAllocator(mapService, new LoggedDatabase(config));
			mobilizator = new AmbulanceMobilizator(new Database(config));
			trafficJamReallocator = new TrafficJamReallocator(mapService, new LoggedDatabase(config));
			cancelator = new Cancelator(new Database(config));

		}

		internal void Start()
		{
			Task.Factory.StartNew(delegate {
				while (true) {
					Check();
					Thread.Sleep(TimeSpan.FromSeconds(6));
				}
			});
		}

		internal void NewIncident(Incident i)
		{
			try {
				allocator.Allocate(i);
			} catch (Exception e) {
				logger.Error("Error in NewIncident(Incident i)");
				logger.Error(e.Message);
			}
		}

		public void MobilizeAmbulance(Incident i)
		{
			try {
				mobilizator.Mobilize(i);
			} catch (Exception e) {
				logger.Error("Error in MobilizeAmbulance(Incident i)");
				logger.Error(e.Message);
			}
		}

		internal void Check()
		{
			try {
				logger.Info("Checking...");

				foreach (var inc in incidentRepository.UnallocatedIncident().ToArray()) {
					logger.Info("Unallocated incident found");
					NewIncident(inc);
				}

				var enumerable = allocationRepository.UnmobilizedAllocations().ToArray();
				foreach (var inc in enumerable) {
					logger.Info("Not mobilized incident found");
					MobilizeAmbulance(inc);
				}

				//foreach (var inc in allocationRepository.OpenIncidentsWithAmbulanceInTrafficJam().ToArray ()) {
				//	logger.Info("Allocated ambulance in traffic jam");
				//	try {
				//		new Task(() => trafficJamReallocator.Allocate(inc)).Start();
				//	} catch (Exception e) {
				//		logger.Info(e.Message);
				//	}
				//}

				foreach (var inc in allocationRepository.MobilizationToCancel().ToArray()) {
					logger.Info("Mobilization to cancel");
					try {
						new Task(() => cancelator.Cancel(inc)).Start();
					} catch (Exception e) {
						logger.Info(e.Message);
					}
				}

				incidentRepository.ResolveUnreachableIncidents();

			} catch (Exception e) {
				logger.Info("Error in check");
				logger.Info(e.Message);
				logger.Info(e.StackTrace);
			}
		}
	}
}
