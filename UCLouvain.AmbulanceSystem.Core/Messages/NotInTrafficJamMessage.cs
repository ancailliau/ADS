using System;
namespace UCLouvain.AmbulanceSystem.Core.Messages
{
	public class NotInTrafficJamMessage : Message
	{
		public string AmbulanceId {
			get;
			set;
		}

		public NotInTrafficJamMessage()
		{
		}

		public NotInTrafficJamMessage(string ambulanceId)
		{
			AmbulanceId = ambulanceId;
		}
	}
}
