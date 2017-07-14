using System;
using UCLouvain.AmbulanceSystem.Core.Domain;

namespace UCLouvain.AmbulanceSystem.Core.Messages
{
	public class AmbulanceStatusMessage : Message
	{
		public string AmbulanceIdentifier {
			get;
			set;
		}

		public AmbulanceStatus Status {
			get;
			set;
		}

		public AmbulanceStatusMessage()
		{
		}

		public AmbulanceStatusMessage(string ambulanceIdentifier, AmbulanceStatus status)
		{
			AmbulanceIdentifier = ambulanceIdentifier;
			Status = status;
		}

		public override string ToString()
		{
			return string.Format("[AmbulanceStatusMessage: AmbulanceIdentifier={0}, Status={1}]", AmbulanceIdentifier, Status);
		}
	}
}
