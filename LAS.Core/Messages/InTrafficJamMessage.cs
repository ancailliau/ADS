using System;
namespace UCLouvain.AmbulanceSystem.Core.Messages
{
	public class InTrafficJamMessage : Message
	{
		public string AmbulanceId {
			get;
			set;
		}

		public InTrafficJamMessage()
		{
		}

		public InTrafficJamMessage(string ambulanceId)
		{
			AmbulanceId = ambulanceId;
		}
	}
}
