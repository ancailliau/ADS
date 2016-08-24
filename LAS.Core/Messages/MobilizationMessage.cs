using System;
namespace LAS.Core.Messages
{
	public class MobilizationMessage : Message
	{
		public string AmbulanceIdentifier {
			get;
			set;
		}

		public int AllocationId {
			get;
			set;
		}

		public float IncidentLatitude {
			get;
			set;
		}

		public float IncidentLongitude {
			get;
			set;
		}

		public string HospitalIdentifier {
			get;
			set;
		}

		public MobilizationMessage() : base ()
		{
		}

		public MobilizationMessage(string ambulanceIdentifier, 
		                           int allocationId, 
		                           float incidentLatitude, 
		                           float incidentLongitude,
		                          string hospitalIdentifier)
		{
			AmbulanceIdentifier = ambulanceIdentifier;
			AllocationId = allocationId;
			IncidentLatitude = incidentLatitude;
			IncidentLongitude = incidentLongitude;
			HospitalIdentifier = hospitalIdentifier;
		}

		public override string ToString()
		{
			return string.Format("[MobilizationMessage: AmbulanceIdentifier={0}, AllocationId={1}, HospitalIdentifier={2}]", AmbulanceIdentifier, AllocationId, HospitalIdentifier);
		}
	}
}
