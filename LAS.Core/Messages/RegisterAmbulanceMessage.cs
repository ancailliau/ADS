using System;
namespace LAS.Core.Messages
{
	public class RegisterAmbulanceMessage : Message
	{
		public string Identifier {
			get;
			set;
		}

		public string ListeningQueue {
			get;
			set;
		}

		public float Latitude {
			get;
			set;
		}

		public float Longitude {
			get;
			set;
		}

		public RegisterAmbulanceMessage()
			: base()
		{
		}

		public RegisterAmbulanceMessage(string identifier, string listening_queue,
		                               float latitude, float longitude)
			: base ()
		{
			Identifier = identifier;
			ListeningQueue = listening_queue;
			Latitude = latitude;
			Longitude = longitude;
		}

		public override string ToString()
		{
			return string.Format("[RegisterAmbulanceMessage: Identifier={0}, ListeningPort={1}, Latitude={2}, Longitude={3}]", Identifier, ListeningQueue, Latitude, Longitude);
		}
	}
}
