using System;
using LAS.Core.Messages;
namespace LAS.Server
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
