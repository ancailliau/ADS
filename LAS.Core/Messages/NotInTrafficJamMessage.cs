using System;
namespace LAS.Core.Messages
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
