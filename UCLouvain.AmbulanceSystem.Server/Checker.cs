﻿using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UCLouvain.AmbulanceSystem.Core.Domain;
using UCLouvain.AmbulanceSystem.Core.Repository;
using UCLouvain.AmbulanceSystem.Core.Utils;
using UCLouvain.AmbulanceSystem.Server.Allocators;
using NLog;
using PetaPoco;

namespace UCLouvain.AmbulanceSystem.Server
{
	public class Checker
	{
		static readonly Logger logger = LogManager.GetCurrentClassLogger();

		IAmbulanceAllocator allocator;
		AmbulanceMobilizator mobilizator;
		TrafficJamReallocator trafficJamReallocator;
		Cancelator cancelator;

		internal readonly AmbulanceRepository ambulanceRepository;
		internal readonly HospitalRepository hospitalRepository;
		internal readonly IncidentRepository incidentRepository;
		internal readonly AllocationRepository allocationRepository;
		internal readonly ConfigurationRepository configurationRepository;

		internal MapService mapService;
		IDatabaseBuildConfiguration config;

		public Checker(IDatabaseBuildConfiguration config)
		{
			this.config = config;
			var db = new Database(config);
			ambulanceRepository = new AmbulanceRepository(db);
			hospitalRepository = new HospitalRepository(db);
			incidentRepository = new IncidentRepository(db);
			allocationRepository = new AllocationRepository(db);
			configurationRepository = new ConfigurationRepository(db);

			mapService = new MapService();

			allocator = new DefaultAmbulanceAllocator(mapService, new LoggedDatabase(config));
			configurationRepository.UpdateActiveAllocator("DefaultAmbulanceAllocator");

			mobilizator = new AmbulanceMobilizator(new Database(config));
			trafficJamReallocator = new TrafficJamReallocator(mapService, new LoggedDatabase(config));
			cancelator = new Cancelator(new Database(config));

		}

		internal void ReplaceAllocator(string s)
		{
			logger.Info("Replace allocator: " + s);
			if (s == "AmbulanceAtStationAllocator") {
				allocator = new AmbulanceAtStationAllocator(mapService, new LoggedDatabase(config));
				configurationRepository.UpdateActiveAllocator(s);

			} else if (s == "DefaultAmbulanceAllocator") {
				allocator = new DefaultAmbulanceAllocator(mapService, new LoggedDatabase(config));
				configurationRepository.UpdateActiveAllocator(s);

			} else {
				logger.Info("Allocator name not known");
			}
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
