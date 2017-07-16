using System;
using System.Collections.Concurrent;
using System.Threading;
using Itinero;
using Itinero.Exceptions;
using UCLouvain.AmbulanceSystem.Core.Domain;
using UCLouvain.AmbulanceSystem.Core.Repository;
using UCLouvain.AmbulanceSystem.Core.Utils;
using NLog;
using PetaPoco;

namespace UCLouvain.AmbulanceSystem.Server.Allocators
{
	public class TrafficJamReallocator
	{
		static readonly Logger logger = LogManager.GetCurrentClassLogger();

		readonly AmbulanceRepository ambulanceRepository;
		readonly HospitalRepository hospitalRepository;
		readonly IncidentRepository incidentRepository;
		readonly AllocationRepository allocationRepository;

		readonly MapService mapService;
		BlockingCollection<Incident> incidentsToProcess;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:UCLouvain.AmbulanceSystem.Server.AmbulanceAllocator"/> class.
		/// </summary>
		/// <param name="db">Db. Do not share among threads.</param>
		public TrafficJamReallocator(MapService mapService, IDatabase db)
		{
			ambulanceRepository = new AmbulanceRepository(db);
			hospitalRepository = new HospitalRepository(db);
			incidentRepository = new IncidentRepository(db);
			allocationRepository = new AllocationRepository(db);

			this.mapService = mapService;
			this.incidentsToProcess = new BlockingCollection<Incident>();

			new Thread(this.Start).Start();
		}

		internal void Allocate(Incident i)
		{
			incidentsToProcess.Add(i);
		}

		void Start()
		{
			foreach (var i in incidentsToProcess.GetConsumingEnumerable()) {
				logger.Info("try to reallocate incident");
				var existingAllocation = allocationRepository.GetAllocationForIncident(i.IncidentId);

				RouterPoint incidentPoint;
				try {
					incidentPoint = mapService.ResolvePoint(i.Latitude, i.Longitude);

				} catch (ResolveFailedException) {
					logger.Info("Incident unreachable");
					incidentRepository.MarkUnreachable(i.IncidentId);
					continue;
				}

				try {
					// Compute timing from ambulance position to incident. Take inTraffic into account. 
					var timing = -1f;
					RouterPoint ambulancePoint;
					Route route;
					try {
						var currentAmbulance = ambulanceRepository.Get(existingAllocation.AmbulanceId);
						ambulancePoint = mapService.ResolvePoint(currentAmbulance.Latitude, currentAmbulance.Longitude);
						route = mapService.Calculate(ambulancePoint, incidentPoint);
						timing = route.TotalTime * 3; // inTraffic penalty

					} catch (ResolveFailedException) {
						logger.Info("Ambulance position cannot be resolved.");
						timing = -1f;
					}

					// Search for a better ambulance
					Ambulance closestAmbulance = null;
					foreach (var ambulance in ambulanceRepository.GetAvailableAmbulances(existingAllocation.IncidentId)) {
						try {
							ambulancePoint = mapService.ResolvePoint(ambulance.Latitude, ambulance.Longitude);
						} catch (ResolveFailedException) {
							logger.Info("Ambulance position cannot be resolved.");
							continue;
						}

						try {
							route = mapService.Calculate(ambulancePoint, incidentPoint);
						} catch (RouteNotFoundException) {
							continue;
						}

						if (!(timing > 0) || route.TotalTime < timing) {
							closestAmbulance = ambulance;
							timing = route.TotalTime;
						}
					}

					if (closestAmbulance == null) {
						logger.Info("No better available ambulance can reach the incident {0}", i.IncidentId);
						continue;
					} else {
						logger.Info("Better ambulance found to reach the incident {0}", i.IncidentId);
					}

					Hospital closestHospital = null;
					timing = -1f;
					foreach (var hospital in hospitalRepository.GetOpenHospitals()) {
						RouterPoint hospitalPoint;
						try {
							hospitalPoint = mapService.ResolvePoint(hospital.Latitude, hospital.Longitude);
						} catch (ResolveFailedException) {
							logger.Info("Hospital position cannot be resolved.");
							hospitalRepository.Close(hospital.HospitalId);
							continue;

						}

						try {
							route = mapService.Calculate(incidentPoint, hospitalPoint);
						} catch (RouteNotFoundException) {
							continue;
						}

						if (!(timing > 0) || route.TotalTime < timing) {
							closestHospital = hospital;
							timing = route.TotalTime;
						}
					}

					if (closestHospital == null) {
						logger.Info("No hospital can be reached from incident {0}", i.IncidentId);
						incidentRepository.MarkUnreachable(i.IncidentId);
					}

					allocationRepository.CancelAllocation(existingAllocation.AllocationId, false);

					var allocationId = incidentRepository.Allocate(i.IncidentId,
																   closestAmbulance.AmbulanceId,
																   closestHospital.HospitalId);
					logger.Info("Allocated ambulance {0} on incident {1} (allocation {3})",
								i.IncidentId, closestAmbulance.AmbulanceId, allocationId);

				} catch (Exception e) {
					logger.Error("Error while allocation: " + e.Message);
					logger.Error(e.StackTrace);
				}
			}
		}
	}
}
