using System;
using UCLouvain.AmbulanceSystem.Core.Messages;
namespace UCLouvain.AmbulanceSystem.Server
{
	public class IncidentProcessor
	{
		Orchestrator orchestrator;

		public IncidentProcessor(Orchestrator o)
		{
			orchestrator = o;
		}

		public void Process(IncidentForm i)
		{
			var i2 = orchestrator.incidentRepository.AddIncident(i.Latitude, i.Longitude);
			// orchestrator.NewIncident(i2);
		}
	}
}
