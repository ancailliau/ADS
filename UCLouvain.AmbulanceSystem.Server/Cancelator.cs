using System;
using System.Collections.Concurrent;
using UCLouvain.AmbulanceSystem.Core.Repository;
using UCLouvain.AmbulanceSystem.Core.Utils;
using NLog;
using UCLouvain.AmbulanceSystem.Core.Domain;
using UCLouvain.AmbulanceSystem.Core.Messages;
using PetaPoco;
using System.Threading;

namespace UCLouvain.AmbulanceSystem.Server
{
	public class Cancelator
	{
		static readonly Logger logger = LogManager.GetCurrentClassLogger();

		readonly AmbulanceRepository ambulanceRepository;
		readonly HospitalRepository hospitalRepository;
		readonly IncidentRepository incidentRepository;
		readonly AllocationRepository allocationRepository;

		readonly RabbitMQMessageSender sender;
		readonly BlockingCollection<Allocation> allocationsToProcess;

		public Cancelator(IDatabase db)
		{
			ambulanceRepository = new AmbulanceRepository(db);
			hospitalRepository = new HospitalRepository(db);
			incidentRepository = new IncidentRepository(db);
			allocationRepository = new AllocationRepository(db);

			sender = new RabbitMQMessageSender();
			allocationsToProcess = new BlockingCollection<Allocation>();

			new Thread(Start).Start();
		}

		internal void Cancel(Allocation i)
		{
			allocationsToProcess.Add(i);
		}

		void Start()
		{
			foreach (var i in allocationsToProcess.GetConsumingEnumerable()) {
				var ambulance = ambulanceRepository.Get(i.AmbulanceId);
				var m = new MobilizationCancelled(i.AllocationId);

				sender.Send(m, ambulance.Port);
			}
		}
	}
}
