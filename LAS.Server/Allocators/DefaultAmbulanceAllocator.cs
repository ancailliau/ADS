using System;
using System.Linq;
using System.Threading;
using LAS.Core.Domain;
using NLog;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using LAS.Core.Repository;
using PetaPoco;
using LAS.Core.Utils;
using Itinero.Exceptions;
using Itinero;
using System.Collections.Generic;

namespace LAS.Server.Allocators
{
	/// <summary>
	/// This module is responsible for allocating ambulance in its own thread.
	/// </summary>
	public class DefaultAmbulanceAllocator : IAmbulanceAllocator
	{
		static readonly Logger logger = LogManager.GetCurrentClassLogger();

		readonly AmbulanceRepository ambulanceRepository;
		readonly HospitalRepository hospitalRepository;
		readonly IncidentRepository incidentRepository;

		readonly MapService mapService;
		BlockingCollection<Incident> incidentsToProcess;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:LAS.Server.AmbulanceAllocator"/> class.
		/// </summary>
		/// <param name="db">Db. Do not share among threads.</param>
		public DefaultAmbulanceAllocator(MapService mapService, IDatabase db)
		{
			ambulanceRepository = new AmbulanceRepository(db);
			hospitalRepository = new HospitalRepository(db);
			incidentRepository = new IncidentRepository(db);

			this.mapService = mapService;
			this.incidentsToProcess = new BlockingCollection<Incident>();

			new Thread(this.Start).Start();
		}

		public void Allocate(Incident i)
		{
			incidentsToProcess.Add(i);
		}

		void Start()
		{
			foreach (var i in incidentsToProcess.GetConsumingEnumerable()) {
				RouterPoint incidentPoint;
				try {
					incidentPoint = mapService.ResolvePoint(i.Latitude, i.Longitude);

				} catch (ResolveFailedException) {
					logger.Info("Incident unreachable");
					incidentRepository.MarkUnreachable(i.IncidentId);
					continue;
				}

				try {
					Ambulance closestAmbulance = null;
					var timing = -1f;
					var ambulances = ambulanceRepository.GetAvailableAmbulances(i.IncidentId);
					GetClosestAmbulance(incidentPoint, ref closestAmbulance, ref timing, ambulances);

					if (closestAmbulance == null) {
						logger.Info("No available ambulance can reach the incident {0}", i.IncidentId);
						logger.Info("Trying without taking existing allocations for that incident into account.");
						ambulances = ambulanceRepository.GetAvailableAmbulances();
						GetClosestAmbulance(incidentPoint, ref closestAmbulance, ref timing, ambulances);

						if (closestAmbulance == null) {
							continue;
						}
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

						Route route;
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

					logger.Info("Allocated ambulance {0} on incident {1} for hospital {2}",
								closestAmbulance.AmbulanceId, i.IncidentId, closestHospital.HospitalId);
					var allocationId = incidentRepository.Allocate(i.IncidentId,
																   closestAmbulance.AmbulanceId,
																   closestHospital.HospitalId);
					logger.Info("Allocation id = " + allocationId);

				} catch (Exception e) {
					logger.Error("Error while allocation: " + e.Message);
					logger.Error(e.StackTrace);
				}
			}
		}

		void GetClosestAmbulance(RouterPoint incidentPoint, 
		                         ref Ambulance closestAmbulance, 
		                         ref float timing, 
		                         IEnumerable<Ambulance> ambulances)
		{
			foreach (var ambulance in ambulances) {
				RouterPoint ambulancePoint;
				try {
					ambulancePoint = mapService.ResolvePoint(ambulance.Latitude, ambulance.Longitude);
				} catch (ResolveFailedException) {
					logger.Info("Ambulance position cannot be resolved.");
					continue;

				}

				Route route;
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
		}
}
}
