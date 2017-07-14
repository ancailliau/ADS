using System;
using UCLouvain.AmbulanceSystem.Core.Domain;
using UCLouvain.AmbulanceSystem.Core.Messages;
using UCLouvain.AmbulanceSystem.Core.Utils;
using NLog;
using System.Collections.Concurrent;
using System.Threading;
using UCLouvain.AmbulanceSystem.Core.Repository;
using PetaPoco;
using System.Linq;

namespace UCLouvain.AmbulanceSystem.Server
{
	/// <summary>
	/// This module is responsible for mobilizing ambulances in its own thread. 
	/// </summary>
	public class AmbulanceMobilizator
	{
		static readonly Logger logger = LogManager.GetCurrentClassLogger();

		readonly AmbulanceRepository ambulanceRepository;
		readonly HospitalRepository hospitalRepository;
		readonly IncidentRepository incidentRepository;
		readonly AllocationRepository allocationRepository;

		readonly RabbitMQMessageSender sender;
		readonly BlockingCollection<Incident> incidentsToProcess;

		public AmbulanceMobilizator(IDatabase db)
		{
			ambulanceRepository = new AmbulanceRepository(db);
			hospitalRepository = new HospitalRepository(db);
			incidentRepository = new IncidentRepository(db);
			allocationRepository = new AllocationRepository(db);

			sender = new RabbitMQMessageSender();
			incidentsToProcess = new BlockingCollection<Incident>();

			new Thread(Start).Start();
		}

		internal void Mobilize(Incident i)
		{
			incidentsToProcess.Add(i);
		}

		void Start()
		{
			foreach (var i in incidentsToProcess.GetConsumingEnumerable()) {
				var allocations = incidentRepository.GetOpenAllocation(i.IncidentId).ToList ();
				if (allocations.Count() == 0) {
					// could not find the allocation...
					continue;
				}
				Allocation allocation = allocations.First ();
				if (allocations.Count() > 1) {
					logger.Info("Oops, multiple allocations found. Pick one, cancel the others.");
					foreach (var a in allocations.Skip(1)) {
						allocationRepository.CancelAllocation(a.AllocationId, false);
					}
				}

				var ambulance = ambulanceRepository.Get(allocation.AmbulanceId);
				try {

					if (DateTime.Now - allocation.AllocationTimestamp > TimeSpan.FromSeconds(5 * 60 / 10)) {
						logger.Info("Cancel allocation '{0}'", allocation.AllocationId);

						//ambulanceRepository.UpdateStatus(allocation.AmbulanceId,
						//								 AmbulanceStatus.Unavailable);
						allocationRepository.CancelAllocation(allocation.AllocationId, false);

					} else {
						logger.Info("Mobilize ambulance {0} for incident {1}",
									allocation.AmbulanceId,
									allocation.IncidentId);

						var m = new MobilizationMessage(allocation.AmbulanceId,
														allocation.AllocationId,
														i.Latitude,
														i.Longitude,
														allocation.HospitalId);

						if (!sender.Send(m, ambulance.Port)) {
							logger.Info("Unable to contact to ambulance {0}, cancel allocation {1}", 
							            allocation.AmbulanceId,
							            allocation.AllocationId);
							ambulanceRepository.UpdateStatus(allocation.AmbulanceId, 
							                                 AmbulanceStatus.Unavailable);
							allocationRepository.CancelAllocation(allocation.AllocationId, true);
						}
                        
                        OnMobilizationSent(allocation);
                        
                        // TODO OnMobilizationReceived(allocation)
					}

				} catch (Exception e) {
					logger.Error("Error during mobilization: {0}", e.Message);
					logger.Error(e.StackTrace);
					logger.Info("Cancel allocation '{0}'", allocation.AllocationId);
					allocationRepository.CancelAllocation(allocation.AllocationId, true);
				}
			}
		}

		void OnMobilizationSent(Allocation allocation)
		{
			logger.Info("Mobilization Order sent for allocation {0}", allocation.AllocationId); 
			allocationRepository.SetMobilizationSent(allocation);
		}

		void OnMobilizationReceived(Allocation allocation)
		{
			logger.Info("Mobilization Order received for allocation {0}", allocation.AllocationId); 
			allocationRepository.SetMobilizationReceived(allocation);
		}
	}
}
